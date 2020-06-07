using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Models;
using Authentication.Shared.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    /// <summary>
    /// Get resource tokens azure function
    /// This function uses to get the cosmos resource token permissions
    /// </summary>
    public static class GetResourceTokens
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

            // validate client token to prevent spamming 
            var (clientResult, clientMessage, _) = ADAccess.Instance.ValidateClientToken(req.Query["client_token"]);
            if (!clientResult)
            {
                return HttpHelper.CreateErrorResponse(clientMessage);
            }

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

            // get access token by refresh token
            var adToken = await ADAccess.Instance.RefreshToken(refreshToken);
            if(adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return HttpHelper.CreateErrorResponse("refresh token is invalid", StatusCodes.Status401Unauthorized);
            }

            // Validate the access token, then get id
            var (result, message, id) = await ADAccess.Instance.ValidateAccessToken(adToken.AccessToken);
            if (!result)
            {
                return HttpHelper.CreateErrorResponse(message, StatusCodes.Status401Unauthorized);
            }

            // find ad user by its email
            var user = await ADUser.FindById(id);
            if (user == null)
            {
                return HttpHelper.CreateErrorResponse("user not exist");
            }

            // check role of user
            ADGroup userGroup = null;
            var groupIds = await user.GroupIds();
            if (groupIds != null && groupIds.Count > 0)
            {
                var group = await ADGroup.FindById(groupIds[0]);
                if (group != null)
                {
                    userGroup = group;
                }
            }

            if(userGroup == null)
            {
                userGroup = await ADGroup.FindByName(Configurations.AzureB2C.GuestGroup);
            }

            // get group permissions
            var permissions = await userGroup.GetPermissions();

            // get user permissions
            var userPermissions = await user.GetPermissions(userGroup.Name);
            permissions.AddRange(userPermissions);

            // return list of permissions
            return new JsonResult(new { success = true, permissions, group = userGroup.Name, refreshToken = adToken.RefreshToken }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
