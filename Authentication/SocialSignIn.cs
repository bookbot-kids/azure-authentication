using System;
using Authentication.Shared;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Logger = Authentication.Shared.Library.Logger;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class SocialSignIn: BaseFunction
    {
        [FunctionName("SocialSignIn")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
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

            string source = req.Query["source"];
            log.LogInformation($"Check account for user {email}, source: {source}");

            if (source == "google")
            {
                string token = req.Query["token"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    return CreateErrorResponse($"token is missing");
                }

                return await ProcessGoogleRequest(log,token, email, name, country, ipAddress);
            }
            else if (source == "apple")
            {
                string token = req.Query["token"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    return CreateErrorResponse($"token is missing");
                }

                string idToken = req.Query["id_token"];
                if (string.IsNullOrWhiteSpace(token))
                {
                    return CreateErrorResponse($"id_token is missing");
                }
                return await ProcessAppleRequest(log, token, idToken, email, name, country, ipAddress);
            } else
            {
                return CreateErrorResponse("Sign in source is invalid");
            }
        }

        private async Task<IActionResult> ProcessGoogleRequest(ILogger log, string token, string email, string name, string country, string ipAddress)
        {
            // validate token from client
            var isValid = await GoogleService.Instance.ValidateAccessToken(email, token);
            if(!isValid)
            {
                return CreateErrorResponse("Token is invalid", 401);
            }

            // then create or get cognito/b2c user
            return await CreateOrGetCognito(log, email, name, country, ipAddress);
        }

        private async Task<IActionResult> ProcessAppleRequest(ILogger log, string token, string idToken, string email, string name, string country, string ipAddress)
        {
            // validate token from client
            var isValid = await AppleService.Instance.ValidateToken(email, token, idToken);
            if (!isValid.Item1)
            {
                return CreateErrorResponse(isValid.Item2, 401);
            }

            // then create or get cognito/b2c user
            return await CreateOrGetCognito(log, email, name, country, ipAddress);
        }

        private async Task<IActionResult> CreateOrGetCognito(ILogger log, string email, string name, string country, string ipAddress)
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
                var passcode = await CognitoService.Instance.RequestPasscode(email, "en", disableEmail: true);
                return new JsonResult(new { success = true, exist, user, passcode = passcode }) { StatusCode = StatusCodes.Status200OK };
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

            var newPasscode = await CognitoService.Instance.RequestPasscode(email, "en", disableEmail: true);
            // Success, return user info
            return new JsonResult(new { success = true, exist, user, passcode = newPasscode }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}

