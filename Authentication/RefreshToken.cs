using System.Threading.Tasks;
using Authentication.Shared.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Models;

namespace Authentication
{
    /// <summary>
    /// Refresh token azure function
    /// This function uses to refresh the b2c access token by refresh token
    /// </summary>
    public class RefreshToken: BaseFunction
    {
        /// <summary>
        /// A http (Get, Post) method to refresh token<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"refresh_token": The b2c refresh token</description></item>
        /// </list> 
        /// This function will call b2c api to get an access token from refresh token in client
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>Access token result with http code 200 if no error, otherwise return http error</returns> 
        [FunctionName("RefreshToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // set logger instance
            Logger.Log = log;

            // validate refresh token
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return CreateErrorResponse("refresh_token is missing");
            }

            // get access token from refresh token
            var token = await ADAccess.Instance.RefreshToken(refreshToken);
            if (token == null)
            {
                return CreateErrorResponse("Can not refresh token", StatusCodes.Status500InternalServerError);
            }

            // return access token
            return new JsonResult(new { success = true, token }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
