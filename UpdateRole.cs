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
    /// Update user role (group) azure function
    /// This function uses to update user role. It also removes all the existing roles of user before assign to new role
    /// </summary>
    public static class UpdateRole
    {
        /// <summary>
        /// A http (Get, Post) method to update user role<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"auth_token": The authentication token to validate user role. Only admin can call this function</description></item>
        /// <item><description>"email": User email</description></item>
        /// <item><description>"role": user role to update. If user already has this role, then return success</description></item>
        /// </list> 
        /// This function will validate the email, then get ADUser by that email if it is valid.<br/>
        /// All the existing roles of user are removed, then assign user to the new role
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>Http success with code 200 if no error, otherwise return http error</returns> 
        [FunctionName("UpdateRole")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            if(!string.IsNullOrWhiteSpace(req.Query["refresh_token"]))
            {
                // get access token by refresh token
                var adToken = await ADAccess.Instance.RefreshToken(req.Query["refresh_token"]);
                if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
                {
                    return HttpHelper.CreateErrorResponse("refresh token is invalid", StatusCodes.Status401Unauthorized);
                }

                // validate admin token
                var actionResult = await HttpHelper.VerifyAdminToken(adToken.AccessToken);
                if (actionResult != null)
                {
                    return actionResult;
                }
            } else
            {
                // validate auth token
                var actionResult = await HttpHelper.VerifyAdminToken(req.Query["auth_token"]);
                if (actionResult != null)
                {
                    return actionResult;
                }
            }

            // validate user email
            var email = req.Query["email"];
            if (string.IsNullOrWhiteSpace(email))
            {
                return HttpHelper.CreateErrorResponse("email is missing");
            }

            // get user by email
            var user = await ADUser.FindByEmail(email);
            if (user == null)
            {
                return HttpHelper.CreateErrorResponse("email not exist");
            }

            // validate role parameter
            var group = await ADGroup.FindByName(req.Query["role"]);
            if (group == null)
            {
                return HttpHelper.CreateErrorResponse("Role is invalid");
            }

            var result = await user.UpdateGroup(group.Name);
            if(result)
            {
                return HttpHelper.CreateSuccessResponse();
            }

            return HttpHelper.CreateErrorResponse("can not add user into group");
        }
    }
}
