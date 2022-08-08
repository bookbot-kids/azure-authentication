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
                var guestPermissions = await guestGroup.GetPermissions();
                return new JsonResult(new { success = true, permissions = guestPermissions, group = guestGroup.Name }) { StatusCode = StatusCodes.Status200OK };
            }

            string source = req.Query["source"];
            ADGroup userGroup = null;
            ADUser user;
            ADToken adToken;
            // cognito authentication
            if (source == "cognito")
            {
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

                var customUserId = await CognitoService.Instance.GetCustomUserId(userId);

                if(string.IsNullOrWhiteSpace(customUserId))
                {
                    return CreateErrorResponse($"user {userId} does not have custom id", statusCode: StatusCodes.Status500InternalServerError);
                }

                // NOTE: if cognito user is disable, it throws exception on refresh token step above, so may not need to check account status
                //var userInfo = await CognitoService.Instance.GetUserInfo(userId);
                //if (!userInfo.Enabled)
                //{
                //    return CreateErrorResponse("user is disabled", statusCode: StatusCodes.Status401Unauthorized);
                //}

                // create fake ADUser and ADGroup from cognito information
                user = new ADUser { ObjectId = customUserId };
                userGroup = new ADGroup { Name = groupName };
            }
            else
            {
                // azure b2c authentication
                // get access token by refresh token
                adToken = await ADAccess.Instance.RefreshToken(refreshToken);
                if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
                {
                    return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
                }

                // Validate the access token, then get id
                var (result, message, id) = await ADAccess.Instance.ValidateAccessToken(adToken.AccessToken);
                if (!result)
                {
                    log.LogError($"can not get access token from refresh token {refreshToken}");
                    return CreateErrorResponse(message, StatusCodes.Status403Forbidden);
                }

                // find ad user by its email
                user = await ADUser.FindById(id);
                if (user == null)
                {
                    return CreateErrorResponse("user not exist");
                }

                if (!user.AccountEnabled)
                {
                    return CreateErrorResponse("user is disabled", statusCode: StatusCodes.Status401Unauthorized);
                }

                // check role of user
                var groupIds = await user.GroupIds();
                if (groupIds != null && groupIds.Count > 0)
                {
                    var group = await ADGroup.FindById(groupIds[0]);
                    if (group != null)
                    {
                        userGroup = group;
                    }
                }

                if (userGroup == null)
                {
                    userGroup = await ADGroup.FindByName(Configurations.AzureB2C.GuestGroup);
                }
            }


            log.LogInformation($"user {user?.ObjectId} has group {userGroup?.Name}");

            var tasks = new List<Task<List<PermissionProperties>>>();
            // get group permissions
            tasks.Add(userGroup.GetPermissions());

            // get user permissions
            tasks.Add(user.GetPermissions(userGroup.Name));

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
