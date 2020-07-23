using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Utils;
using Extensions;
using Authentication.Shared.Models;

namespace Authentication
{
    /// <summary>
    /// Get user info azure function
    /// This function uses to get user info (User table) from cosmos
    /// </summary>
    public static class GetUserInfo
    {
        /// <summary>
        /// A http (Get, Post) method to get cosmos user record <br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"client_token": The client token to prevent spamming server. This token is generated from client by JWT</description></item>
        /// <item><description>"email": User email</description></item>
        /// </list> 
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>A cosmos User record</returns>
        [FunctionName("GetUserInfo")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            string email = req.Query["email"];

            // validate email address
            if (string.IsNullOrWhiteSpace(email) || !email.IsValidEmailAddress())
            {
                return HttpHelper.CreateErrorResponse("Email is invalid");
            }

            // replace space by + to correct because email contains "+" will be encoded by space, like "a+1@gmail.com" -> "a 1@gmail.com"
            email = email.Trim().Replace(" ", "+");

            var user = await User.GetByEmail(email);
            return new JsonResult(new { success = true, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
