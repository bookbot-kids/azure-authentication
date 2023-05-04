using System.Threading.Tasks;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Authentication.Shared.Services;
using System;
using Authentication.Shared;

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
        public async Task<IActionResult> Run(
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

            var ipAddress = HttpHelper.GetIpFromRequestHeaders(req);
            string country = req.Query["country"];
            email = email.NormalizeEmail();

            string name = email.GetNameFromEmail();
            string appId = req.Query["app_id"];

            string source = req.Query["source"];
            string language = req.Query["language"];
            log.LogInformation($"Check account for user {email}, source: {source}");

            if (source != "cognito")
            {
                log.LogError($"{email} has invalid source {source}");
                throw new Exception($"{email} has invalid source {source}");
            }

            bool requestPasscode = req.Query["request_passcode"] == "true";
            return await ProcessCognitoRequest(log, email, name, country, ipAddress, requestPasscode, language, appId);
        }

        private async Task<IActionResult> ProcessCognitoRequest(ILogger log, string email, string name, string country, string ipAddress, bool requestPasscode, string language, string appId)
        {
            var (exist, user) = await CognitoService.Instance.FindOrCreateUser(email, name, country, ipAddress);
            // there is an error when creating user
            if (user == null)
            {
                return CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
            }

            // if user already has account
            if (exist)
            {
                // update country and ipadress if needed
                var updateParams = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(country))
                {
                    updateParams["custom:country"] = country;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    updateParams["custom:ipAddress"] = ipAddress;
                }

                await CognitoService.Instance.UpdateUser(user.Username, updateParams, !user.Enabled);
                log.LogInformation($"User ${email} exists, {ipAddress}, {country}");
                if(requestPasscode)
                {
                    await CognitoService.Instance.RequestPasscode(email, language, appId: appId);
                }

                return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
            }
            else
            {
                if (!email.EndsWith(Configurations.AzureB2C.EmailTestDomain))
                {
                    try
                    {
                        await SendAnalytics(email, user.Username, country, ipAddress, name);
                    }
                    catch (Exception ex)
                    {
                        if (!(ex is TimeoutException))
                        {
                            log.LogError($"Send analytics error {ex.Message}");
                        }
                    }
                }
                else
                {
                    log.LogInformation($"User ${email} is a test user, skip sending analytics");
                }
            }

            if (requestPasscode)
            {
                await CognitoService.Instance.RequestPasscode(email, language);
            }

            // Success, return user info
            return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }    
}
