using System;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Utils;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    /// <summary>
    /// Creating AD B2C User azure function.<br/>
    /// This function uses to create AD B2C user with its role.
    /// If there is no error, return new user info. Otherwise return http error
    /// </summary>
    [Obsolete("This Azure function is deprecated. Will remove later")]
    public static class CreateADB2CUser
    {
        /// <summary>
        /// A http (Get, Post) method to create AD B2C user.<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"auth_token": The authentication token to validate user role. Only admin can call this function</description></item>
        /// <item><description>"role": user role, it's also group name in b2c</description></item>
        /// <item><description>"email": user email account in b2c. This email is validated before checking</description></item>
        /// <item><description>"name": name of user. If it is empty, email will be used for name</description></item>
        /// </list> 
        /// User is created by email and name, then assign to role (b2c group).<br/>
        /// If there is no error, return that new user. Otherwise, return http error 
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>User result with http code 200 if no error, otherwise return http error</returns>   
        [FunctionName("CreateADB2CUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            // validate auth token, make sure only admin can call this function
            var actionResult = await HttpHelper.VerifyAdminToken(req.Query["auth_token"]);
            if (actionResult != null)
            {
                return actionResult;
            }

            // validate role
            var group = await ADGroup.FindByName(req.Query["role"]);
            if (group == null)
            {
                return HttpHelper.CreateErrorResponse("Role is invalid");
            }

            // validate email address
            string email = req.Query["email"];
            if (string.IsNullOrWhiteSpace(email) || !email.IsValidEmailAddress())
            {
                return HttpHelper.CreateErrorResponse("Email is invalid");
            }

            // replace space by + to correct because email contains "+" will be encoded by space, like "a+1@gmail.com" -> "a 1@gmail.com"
            email = email.Trim().Replace(" ", "+");
            string name = req.Query["name"];

            // get user by email and name. If it doesn't exist, create a new user
            var user = (await ADUser.FindOrCreate(email, name)).user;
            if (user == null || string.IsNullOrWhiteSpace(user.ObjectId))
            {
                return HttpHelper.CreateErrorResponse("Can not create AD user");
            }

            // add user into group
            var groupResult = await group.AddUser(user.ObjectId);
            if (!groupResult)
            {
                return HttpHelper.CreateErrorResponse($"Can not add user {user.ObjectId} into group {group.Id}");
            }

            // Success, retunr user info
            return new JsonResult(new { success = true, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
