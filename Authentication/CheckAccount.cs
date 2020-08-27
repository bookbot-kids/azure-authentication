using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
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
    public class CheckAccount: BaseFunction
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

            string email = req.Query["email"];

            // validate email address
            if (string.IsNullOrWhiteSpace(email))
            {
                return CreateErrorResponse($"Email is empty");
            }

            if (!email.IsValidEmailAddress())
            {
                return CreateErrorResponse($"Email {email} is invalid");
            }

            // Fix the encode issue because email parameter that contains "+" will be encoded by space
            // e.g. client sends "a+1@gmail.com" => Azure function read: "a 1@gmail.com" (= req.Query["email"])
            // We need to replace space by "+" when reading the parameter req.Query["email"]
            // Then the result is correct "a+1@gmail.com"
            email = email.Trim().Replace(" ", "+");

            string name = email.GetNameFromEmail();

            // check if email is existed in b2c. If it is, return that user
            var (exist, user) = await ADUser.FindOrCreate(email, name);
            if (exist)
            {
                return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
            }


            // there is an error when creating user
            if (user == null)
            {
                return CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
            }

            // add user to new group
            var newGroup = await ADGroup.FindByName("new");
            var addResult = await newGroup.AddUser(user.ObjectId);

            // there is an error when add user into new group
            if (!addResult)
            {
                return CreateErrorResponse($"can not add user {email} into new group", StatusCodes.Status500InternalServerError);
            }

            // Success, return user info
            return new JsonResult(new { success = true, exist, user}) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
