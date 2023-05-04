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
        [FunctionName("GetResourceTokens")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            // validate b2c refresh token
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                // default is guest
                var guestGroup = await ADGroup.FindByName(Configurations.AzureB2C.GuestGroup);

                // If the refresh token is missing, then return permissions for guest
                var guestPermissions = await guestGroup.GetPermissions(new List<string>());
                return new JsonResult(new { success = true, permissions = guestPermissions, group = guestGroup.Name }) { StatusCode = StatusCodes.Status200OK };
            }

            string source = req.Query["source"];
            ADUser user;
            ADToken adToken;
            string clientUserId = req.Query["user_id"];
            string syncTablesParams = req.Query["sync_tables"];
            List<string> syncTables;
            if(!string.IsNullOrWhiteSpace(syncTablesParams))
            {
                syncTables = syncTablesParams.Split(",").ToList();
            } else
            {
                syncTables = new List<string>();
            }

            if (source != "cognito")
            {
                log.LogError($"invalid source {source}");
                throw new Exception($"invalid source {source}");
            }

            // cognito authentication
            adToken = await CognitoService.Instance.GetAccessToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
            }

            // Validate the access token, then get id and group name
            var (result, message, userId, groupName) = await CognitoService.Instance.ValidateAccessToken(adToken.AccessToken);
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
                customUserId = await CognitoService.Instance.GetCustomUserId(userId);

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

            // return list of permissions
            return new JsonResult(new { success = true, permissions, group = userGroup.Name, refreshToken = adToken.RefreshToken }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
