using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Utils;
using Authentication.Shared;
using Extensions;
using Authentication.Shared.Models;

namespace Authentication
{
    public static class GetUserInfo
    {
        [FunctionName("GetUserInfo")]
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

            var user = await User.GetByEmail(email);
            return new JsonResult(new { success = true, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
