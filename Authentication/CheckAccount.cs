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

            var ipAddress = HttpHelper.GetIpFromRequestHeaders(req);
            string country = req.Query["country"];
            email = email?.NormalizeEmail();

            string name = email?.GetNameFromEmail();
            string appId = req.Query["app_id"];

            string source = req.Query["source"];
            string language = req.Query["language"];
            log.LogInformation($"Check account for user {email}, source: {source}");
            string signInToken = req.Query["sign_in_token"];
            bool returnPasscode = req.Query["return_passcode"] == "true";
            var autoSignInTokenValid = false;
            if (!string.IsNullOrWhiteSpace(signInToken))
            {
                string userId = req.Query["user_id"];
                if (string.IsNullOrWhiteSpace(signInToken))
                {
                    return CreateErrorResponse("user_id is required with sign_in_token");
                }

                if (!TokenService.IsValidBase64(signInToken))
                {
                    return CreateErrorResponse("sign_in_token is not valid");
                }

                // validate token
                var iv = userId.Substring(0, 16); // iv is first 16 characters from user id
                var decrypted = TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, iv, signInToken);
                if(decrypted == null)
                {
                    return CreateErrorResponse("sign_in_token is not valid");
                }

                var parts = decrypted.Split(";");
                if(parts.Length < 2)
                {
                    return CreateErrorResponse("sign_in_token is not valid");
                }

                if(userId != parts[0])
                {
                    return CreateErrorResponse("sign_in_token is not valid");
                }

                var timestamp = long.Parse(parts[1]);
                DateTime expiryDate = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
                if (DateTime.UtcNow > expiryDate)
                {
                    return CreateErrorResponse("sign_in_token is expired");
                }

                autoSignInTokenValid = true;

            }

            switch (source)
            {
                case "cognito":
                    {
                        // validate email address
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            return CreateErrorResponse($"Email is empty");
                        }

                        if (!email.IsValidEmailAddress())
                        {
                            return CreateErrorResponse($"Email {email} is invalid");
                        }

                        bool requestPasscode = req.Query["request_passcode"] == "true";
                        return await ProcessCognitoRequest(log, email, name, country, ipAddress, requestPasscode, language, appId, autoSignInTokenValid, returnPasscode);
                    }
                case "whatsapp":
                    {
                        if (string.IsNullOrWhiteSpace(language))
                        {
                            return CreateErrorResponse($"language is missing");
                        }

                        string phone = req.Query["phone"];
                        if (string.IsNullOrWhiteSpace(phone))
                        {
                            return CreateErrorResponse($"phone is missing");
                        }

                        phone = phone.NormalizePhone();
                        if (phone.isValidPhone() != true)
                        {
                            return CreateErrorResponse($"phone is invalid");
                        }

                        return await ProcessWhatsappRequest(log, phone, email, name, country, ipAddress, language, appId, autoSignInTokenValid, returnPasscode);
                    }
                default:
                    throw new Exception($"{email} has invalid source {source}");
            }
        }

        private async Task<IActionResult> ProcessWhatsappRequest(ILogger log, string phone, string email, string name, string country, string ipAddress, string language, string appId, bool autoSignInTokenValid, bool returnPasscode)
        {
            var user = await CognitoService.Instance.FindUserByPhone(phone);
            var existing = true;
            var placeholderEmail = phone + Configurations.Whatsapp.PlaceholderEmail;
            var userEmail = string.IsNullOrWhiteSpace(email) ? placeholderEmail : email;
            if (user == null)
            {
                // if user with phone not exist, then create a new one with place holder email
                var (exist, newUser) = await CognitoService.Instance.FindOrCreateUser(userEmail, name, country, ipAddress, phone: phone, forceCreate: string.IsNullOrWhiteSpace(email));
                user = newUser;
                existing = exist;

                if (existing)
                {
                    // update phone, country and ipadress if needed
                    var updateParams = new Dictionary<string, string>();
                    if (!string.IsNullOrWhiteSpace(country) && CognitoService.Instance.GetUserAttributeValue(user, "custom:country") != country)
                    {
                        updateParams["custom:country"] = country;
                    }

                    if (!string.IsNullOrWhiteSpace(ipAddress) && CognitoService.Instance.GetUserAttributeValue(user, "custom:ipAddress") != ipAddress)
                    {
                        updateParams["custom:ipAddress"] = ipAddress;
                    }

                    if (!string.IsNullOrWhiteSpace(phone) && CognitoService.Instance.GetUserAttributeValue(user, "phone_number") != phone)
                    {
                        updateParams["phone_number"] = phone;
                    }

                    await CognitoService.Instance.UpdateUser(user.Username, updateParams, !user.Enabled);
                }
            }
            else
            {
                var accountEmail = CognitoService.Instance.GetUserAttributeValue(user, "email");
                userEmail = accountEmail ?? userEmail;
            }

            if(autoSignInTokenValid && returnPasscode)
            {
                var passcode = await CognitoService.Instance.RequestPasscode(userEmail, language, appId: appId, disableEmail: true, phone: phone, sendType: "whatsapp", returnPasscode: true);
                return new JsonResult(new { success = true, exist = existing, user, passcode }) { StatusCode = StatusCodes.Status200OK };
            }

            // send passcode to whatsapp
            await CognitoService.Instance.RequestPasscode(userEmail, language, appId: appId, disableEmail: true, phone: phone, sendType: "whatsapp");
            log.LogInformation($"Send OTP into whatsapp {phone}");
            // Success, return user info
            return new JsonResult(new { success = true, exist = existing, user }) { StatusCode = StatusCodes.Status200OK };
        }

        private async Task<IActionResult> ProcessCognitoRequest(ILogger log, string email, string name, string country, string ipAddress, bool requestPasscode, string language, string appId, bool autoSignInTokenValid, bool returnPasscode)
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
                if (!string.IsNullOrWhiteSpace(country) && CognitoService.Instance.GetUserAttributeValue(user, "custom:country") != country)
                {
                    updateParams["custom:country"] = country;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress) && CognitoService.Instance.GetUserAttributeValue(user, "custom:ipAddress") != ipAddress)
                {
                    updateParams["custom:ipAddress"] = ipAddress;
                }

                await CognitoService.Instance.UpdateUser(user.Username, updateParams, !user.Enabled);
                log.LogInformation($"User ${email} exists, {ipAddress}, {country}");

                if (autoSignInTokenValid && returnPasscode)
                {
                    var passcode = await CognitoService.Instance.RequestPasscode(email, language, appId: appId, disableEmail: true);
                    return new JsonResult(new { success = true, exist, user, passcode }) { StatusCode = StatusCodes.Status200OK };
                }

                if (requestPasscode)
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

            if (autoSignInTokenValid && returnPasscode)
            {
                var passcode = await CognitoService.Instance.RequestPasscode(email, language, appId: appId, disableEmail: true);
                return new JsonResult(new { success = true, exist, user, passcode }) { StatusCode = StatusCodes.Status200OK };
            }

            if (requestPasscode)
            {
                await CognitoService.Instance.RequestPasscode(email, language, appId: appId);
            }

            // Success, return user info
            return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }    
}
