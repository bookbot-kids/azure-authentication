using System.Net.Mail;
using System.Threading.Tasks;
using Authentication.Shared;
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
    /// Account existing checking azure function.<br/>
    /// This function uses to check whether user email exist in b2c.
    /// If it does, return that user info, otherwise create a new account with that email then add user into "New" group in b2c and return it
    /// </summary>
    public static class CheckAccount
    {
        /// <summary>
        /// A http (Get, Post) method to check whether user account is exist.<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"token": The client token to prevent spamming server. This token is generated from client by JWT</description></item>
        /// <item><description>"email": user email account in b2c. This email is validated before checking</description></item>
        /// </list> 
        /// If a user with that email exist, then return that user, otherwise create a new user and add it into "New" group in b2c and return it
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>User result with http code 200 if no error, otherwise return http error</returns>
        [FunctionName("CheckAccount")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            // validate client token by issuer & subject. Return error if token is invalid
            var (result, message, _) = ADAccess.Instance.ValidateClientToken(req.Query["client_token"]);
            if (!result)
            {
                return HttpHelper.CreateErrorResponse(message);
            }

            string email = req.Query["email"];

            // validate email address
            if (string.IsNullOrWhiteSpace(email) || !email.IsValidEmailAddress())
            {
                return HttpHelper.CreateErrorResponse("Email is invalid");
            }

            // replace space by + to correct because email contains "+" will be encoded by space, like "a+1@gmail.com" -> "a 1@gmail.com"
            email = email.Trim().Replace(" ", "+");

            var name = new MailAddress(email).User;

            // check if email is existed in b2c. If it is, return that user
            var(exist, user) = await ADUser.FindOrCreate(email, name);
            if (exist)
            {
                // Get group of exist user
                var groupdIds = await user.GroupIds();
                string userGroupName = null;
                if (groupdIds != null && groupdIds.Count > 0)
                {
                    var userGroup = await ADGroup.FindById(groupdIds[0]);
                    if (userGroup != null)
                    {
                        userGroupName = userGroup.Name;
                    }   
                }

                return new JsonResult(new { success = true, exist, user, group = userGroupName }) { StatusCode = StatusCodes.Status200OK };
            }

            // there is an error when creating user
            if (user == null)
            {
                return HttpHelper.CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
            }

            // add user to new group
            var newGroup = await ADGroup.FindByName("new");
            var addResult = await newGroup.AddUser(user.ObjectId);

            // there is an error when add user into new group
            if (!addResult)
            {
                return HttpHelper.CreateErrorResponse($"can not add user {email} into new group", StatusCodes.Status500InternalServerError);
            }

            // Success, retunr user info
            return new JsonResult(new { success = true, exist, user, group = "new" }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
