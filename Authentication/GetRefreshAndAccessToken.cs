using System;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    /// <summary>
    /// Get b2c access token and refresh token azure function
    /// This function uses to get a b2c access token and refresh token from email/password or id token (from b2c login)
    /// </summary>
    public class GetRefreshAndAccessToken : BaseFunction
    {
        /// <summary>
        /// A http (Get, Post) method to get user access token and refresh token by email and password or id token (from b2c login).<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"id_token": The id token from b2c login</description></item>
        /// <item><description>"email": user email</description></item>
        /// <item><description>"password": user password</description></item>
        /// </list> 
        /// If id_token exists, then validate it with b2c custom policy (then get its email) and get the b2c access token and refresh token by using its email and generated password <br/>
        /// If email and password exist, then get b2c access token and refresh token by them 
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>access token and refresh token result with http code 200 if no error, otherwise return http error</returns> 
        [FunctionName("GetRefreshAndAccessToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            string idToken = req.Query["id_token"];
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                return await GetFromIdToken(idToken);
            }

            return await GetFromEmailPassword(req.Query["email"], req.Query["password"]);
        }

        /// <summary>
        /// Get b2c access token and refresh token by id token
        /// </summary>
        /// <param name="idToken">id token</param>
        /// <returns>access token and refresh token result with http code 200 if no error, otherwise return http error</returns>
        private static async Task<IActionResult> GetFromIdToken(string idToken)
        {
            // validate id token and get the email
            var (result, message, email) = await ADAccess.Instance.ValidateIdToken(idToken);
            if (!result)
            {
                return CreateErrorResponse(message);
            }

            try
            {
                // using email (and generated password) to get access token and refresh token
                var token = await ADAccess.Instance.GetAccessToken(email);
                if (token == null)
                {
                    return CreateErrorResponse("Can not get access token. The username or password provided in the request are invalid");
                }

                var user = await ADUser.FindByEmail(email);
                var group = "new";
                if (user != null)
                {
                    // Get group of exist user
                    var groupdIds = await user.GroupIds();
                    if (groupdIds != null && groupdIds.Count > 0)
                    {
                        var userGroup = await ADGroup.FindById(groupdIds[0]);
                        if (userGroup != null)
                        {
                            group = userGroup.Name;
                        }
                    }
                }

                // return access token and refresh token result
                return new JsonResult(new { success = true, token, group }) { StatusCode = StatusCodes.Status200OK };
            }
            catch (Exception)
            {
                return CreateErrorResponse("Can not generate access token and refresh token", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get b2c access token and refresh token from email and password
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="password">user password</param>
        /// <returns>access token and refresh token result with http code 200 if no error, otherwise return http error</returns>
        private static async Task<IActionResult> GetFromEmailPassword(string email, string password)
        {
            // validate email and password
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return CreateErrorResponse("email & password are required");
            }

            // get b2c access token and refresh token by using email and password
            var token = await ADAccess.Instance.GetAccessToken(email, password);
            if (token == null)
            {
                return CreateErrorResponse("Cannot get access token and refresh token. Make sure email and password are correct", StatusCodes.Status500InternalServerError);
            }

            // return access token and refresh token result
            return new JsonResult(new { success = true, token }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
