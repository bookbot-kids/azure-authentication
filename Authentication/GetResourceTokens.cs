using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Services;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using System.Linq;
using System;

namespace Authentication
{
    /// <summary>
    /// Get resource tokens azure function
    /// This function uses to get the cosmos resource token permissions
    /// </summary>
    public class GetResourceTokens: BaseFunction
    {
        /// <summary>
        /// A http (Get, Post) method to get cosmos resource token permissions<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"token": The client token to prevent spamming server. This token is generated from client by JWT</description></item>
        /// <item><description>"access_token": The b2c access token</description></item>
        /// </list> 
        /// If the access_token is missing, then return the guest permissions from cosmos
        /// Otherwise validate the access token, then get user and email from the access token and finally get the cosmos permission for that user  
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>Cosmos resource tokens result with http code 200 if no error, otherwise return http error</returns> 
        /// <summary>
        /// Swaps each qualified table's permission for one scoped to the requested
        /// partition, in place.
        /// </summary>
        /// <remarks>
        /// The unqualified permission has to exist first: that is the role check, so
        /// a qualifier can never widen what a role may read. Shared by the guest and
        /// signed-in paths - a signed-out child reads the same books as a signed-in
        /// one, and having two copies of this would mean only one of them stayed
        /// correct.
        /// </remarks>
        private static async Task ApplyPartitionQualifiers(
            List<PermissionProperties> permissions,
            Dictionary<string, string> qualifiedTables,
            string roleName,
            ILogger log)
        {
            if (permissions == null || qualifiedTables.Count == 0)
            {
                return;
            }

            foreach (var qualified in qualifiedTables)
            {
                var table = qualified.Key;
                var partition = qualified.Value;
                var granted = permissions.FirstOrDefault(p => p.Id == table);
                if (granted == null)
                {
                    log.LogInformation($"role {roleName} has no permission for table {table}, skip partition {partition}");
                    continue;
                }

                var permissionId = $"{table}-{partition}";
                var scoped = await CosmosService.Instance.GetPermission(roleName, permissionId)
                    ?? await CosmosService.Instance.CreatePermission(
                        roleName,
                        permissionId,
                        granted.PermissionMode == PermissionMode.Read,
                        table,
                        partition);

                if (scoped == null)
                {
                    // Leaving the unscoped permission in place is not a safe
                    // fallback: it reads a different partition, so the client sees
                    // an empty table rather than an error.
                    log.LogWarning($"can not create permission {permissionId} for {roleName}, {table} will read partition {granted.ResourcePartitionKey} instead of {partition}");
                    continue;
                }

                // The default-partition token for this table is of no use to a
                // client that asked for a specific partition.
                permissions.Remove(granted);
                permissions.Add(scoped);
            }
        }

        [FunctionName("GetResourceTokens")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            string syncTablesParams = req.Query["sync_tables"];
            List<string> syncTables;

            // A sync_tables entry may carry a qualifier - "Book:bookbot" - meaning
            // "the Book container, scoped to the bookbot partition". The table name
            // is what the role check filters on, so qualifiers are stripped here and
            // applied afterwards when the permission is minted.
            //
            // Parsed before the guest branch below, because a signed-out child
            // reads the same books as a signed-in one.
            var qualifiedTables = new Dictionary<string, string>();
            if(!string.IsNullOrWhiteSpace(syncTablesParams))
            {
                syncTables = new List<string>();
                foreach (var entry in syncTablesParams.Split(","))
                {
                    var parts = entry.Split(":");
                    var table = parts[0].Trim();
                    if (string.IsNullOrWhiteSpace(table))
                    {
                        continue;
                    }

                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        qualifiedTables[table] = parts[1].Trim();
                    }

                    if (!syncTables.Contains(table))
                    {
                        syncTables.Add(table);
                    }
                }
            } else
            {
                syncTables = new List<string>();
            }

            // validate b2c refresh token
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                // default is guest
                var guestGroup = new ADGroup() { Name = Configurations.AzureB2C.GuestGroup };

                // If the refresh token is missing, then return permissions for guest.
                //
                // Deliberately unfiltered. Filtering by syncTables here would be
                // consistent with the signed-in path below, but it would also stop
                // sending existing clients permissions they receive today - and a
                // guest permission that quietly stops arriving shows up as a table
                // that syncs nothing, not as an error. The qualifier below is the
                // part that was actually missing.
                var guestPermissions = await guestGroup.GetPermissions(new List<string>());
                await ApplyPartitionQualifiers(guestPermissions, qualifiedTables, guestGroup.Name, log);
                return new JsonResult(new { success = true, permissions = guestPermissions, group = guestGroup.Name }) { StatusCode = StatusCodes.Status200OK };
            }

            string source = req.Query["source"];
            ADUser user;
            ADToken adToken;
            string clientUserId = req.Query["user_id"];

            if (source != "cognito")
            {
                log.LogError($"invalid source {source} for token {refreshToken}");
                return CreateErrorResponse($"invalid source {source} for token {refreshToken}", StatusCodes.Status400BadRequest);
            }

            // cognito authentication
            adToken = await AWSService.Instance.GetAccessToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
            }

            // Validate the access token, then get id and group name
            var (result, message, userId, groupName) = await AWSService.Instance.ValidateAccessToken(adToken.AccessToken);
            if (!result)
            {
                log.LogError($"can not get access token from refresh token {refreshToken}");
                return CreateErrorResponse(message, StatusCodes.Status403Forbidden);
            }

            string customUserId;
            if (!string.IsNullOrWhiteSpace(clientUserId))
            {
                customUserId = clientUserId;
            }
            else
            {
                customUserId = await AWSService.Instance.GetCustomUserId(userId);

                if (string.IsNullOrWhiteSpace(customUserId))
                {
                    return CreateErrorResponse($"user {userId} does not have custom id", statusCode: StatusCodes.Status500InternalServerError);
                }
            }


            // NOTE: if cognito user is disable, it throws exception on refresh token step above, so may not need to check account status
            //var userInfo = await CognitoService.Instance.GetUserInfo(userId);
            //if (!userInfo.Enabled)
            //{
            //    return CreateErrorResponse("user is disabled", statusCode: StatusCodes.Status401Unauthorized);
            //}

            // create fake ADUser and ADGroup from cognito information
            user = new ADUser { ObjectId = customUserId };
            ADGroup userGroup = new ADGroup { Name = groupName };


            log.LogInformation($"user {user?.ObjectId} has group {userGroup?.Name}");

            var tasks = new List<Task<List<PermissionProperties>>>();
            // get group permissions
            tasks.Add(userGroup.GetPermissions(syncTables));

            // get user permissions
            tasks.Add(user.GetPermissions(userGroup.Name, syncTables));

            await Task.WhenAll(tasks);
            var permissions = new List<PermissionProperties>();
            foreach (var task in tasks)
            {
                var p = task.Result;
                permissions.AddRange(p);
            }

            await ApplyPartitionQualifiers(permissions, qualifiedTables, userGroup.Name, log);

            // return list of permissions
            return new JsonResult(new { success = true, permissions, group = userGroup.Name, refreshToken = adToken.RefreshToken }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
