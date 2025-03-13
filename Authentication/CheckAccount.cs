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
            string firstName = req.Query["first_name"];
            string lastName = req.Query["last_name"];
            var name = StringHelper.CombineName(firstName, lastName);
            string appId = req.Query["app_id"];

            string source = req.Query["source"];
            string language = req.Query["language"];
            string linkType = req.Query["link_type"];
            string os = req.Query["os"];
            log.LogInformation($"Check account for user {email}, source: {source}");
            string signInToken = req.Query["sign_in_token"];
            var autoSignInTokenValid = false;
            if (!string.IsNullOrWhiteSpace(signInToken))
            {
                string userId = req.Query["user_id"];
                if (string.IsNullOrWhiteSpace(userId))
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
                        return await ProcessCognitoRequest(log, email, name, country, ipAddress, requestPasscode, language, appId, signInToken, autoSignInTokenValid, linkType, os);
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

                        log.LogInformation($"Check account for user {phone}, source: {source}");
                        return await ProcessWhatsappRequest(log, phone, email, name, country, ipAddress, language, appId, signInToken, autoSignInTokenValid, linkType, os);
                    }
                default:
                    throw new Exception($"{email} has invalid source {source}");
            }
        }

        private async Task<IActionResult> ProcessWhatsappRequest(ILogger log, string phone, string email, string name, string country, string ipAddress, string language, string appId, string signInToken, bool autoSignInTokenValid, string linkType, string os)
        {
            var user = await AWSService.Instance.FindUserByPhone(phone);
            var existing = true;
            var placeholderEmail = phone + Configurations.Whatsapp.PlaceholderEmail;
            var userEmail = string.IsNullOrWhiteSpace(email) ? placeholderEmail : email;
            log.LogInformation($"ProcessWhatsappRequest {userEmail}, phone: {phone}");
            if (user == null)
            {
                // if user with phone not exist, then create a new one with place holder email
                var (exist, newUser) = await AWSService.Instance.FindOrCreateUser(userEmail, name, country, ipAddress, phone: phone, forceCreate: string.IsNullOrWhiteSpace(email));
                user = newUser;
                existing = exist;

                if (existing)
                {
                    // update phone, country and ipadress if needed
                    var updateParams = new Dictionary<string, string>();
                    if (!string.IsNullOrWhiteSpace(country) && AWSService.Instance.GetUserAttributeValue(user, "custom:country") != country)
                    {
                        updateParams["custom:country"] = country;
                    }

                    if (!string.IsNullOrWhiteSpace(ipAddress) && AWSService.Instance.GetUserAttributeValue(user, "custom:ipAddress") != ipAddress)
                    {
                        updateParams["custom:ipAddress"] = ipAddress;
                    }

                    if (!string.IsNullOrWhiteSpace(phone) && AWSService.Instance.GetUserAttributeValue(user, "phone_number") != phone)
                    {
                        updateParams["phone_number"] = phone;
                    }

                    if (!string.IsNullOrWhiteSpace(language) && AWSService.Instance.GetUserAttributeValue(user, "custom:language") != language)
                    {
                        updateParams["custom:language"] = language;
                    }

                    if (!string.IsNullOrWhiteSpace(os) && AWSService.Instance.GetUserAttributeValue(user, "custom:os") != os)
                    {
                        updateParams["custom:os"] = os;
                    }

                    await AWSService.Instance.UpdateUser(user.Username, updateParams, !user.Enabled);
                }
            }
            else
            {
                var accountEmail = AWSService.Instance.GetUserAttributeValue(user, "email");
                if(accountEmail != null && !string.IsNullOrWhiteSpace(email) && accountEmail != userEmail)
                {
                    return CreateErrorResponse($"phone number is existing on another account");
                }

                userEmail = accountEmail ?? userEmail;
            }

            if(autoSignInTokenValid)
            {
                // check sign in token is in expired list
                var currentTokens = AWSService.Instance.GetUserAttributeValue(user, "custom:tokens");
                if (currentTokens?.Contains(signInToken) == true)
                {
                    return CreateErrorResponse("sign_in_token attribute is expired");
                }

                AWSService.Instance.RemoveAttribute(user, "custom:tokens");
                var passcode = await AWSService.Instance.RequestPasscode(userEmail, language, appId: appId, phone: phone, sendType: "whatsapp", returnPasscode: true, linkType: linkType);
                return new JsonResult(new { success = true, exist = existing, user, passcode }) { StatusCode = StatusCodes.Status200OK };
            }

            // send passcode to whatsapp
            await AWSService.Instance.RequestPasscode(userEmail, language, appId: appId, phone: phone, sendType: "whatsapp", linkType: linkType);
            log.LogInformation($"Send OTP into whatsapp {phone}");
            // Success, return user info
            AWSService.Instance.RemoveAttribute(user, "custom:tokens");
            return new JsonResult(new { success = true, exist = existing, user }) { StatusCode = StatusCodes.Status200OK };
        }

        private async Task<IActionResult> ProcessCognitoRequest(ILogger log, string email, string name, string country, string ipAddress, bool requestPasscode, string language, string appId, string signInToken, bool autoSignInTokenValid, string linkType, string os)
        {
            var (exist, user) = await AWSService.Instance.FindOrCreateUser(email, name, country, ipAddress,language: language, os: os);
            // there is an error when creating user
            if (user == null)
            {
                return CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
            }

            // if user already has account
            if (exist)
            {
                // update properties if needed
                var updateParams = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(country) && AWSService.Instance.GetUserAttributeValue(user, "custom:country") != country)
                {
                    updateParams["custom:country"] = country;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress) && AWSService.Instance.GetUserAttributeValue(user, "custom:ipAddress") != ipAddress)
                {
                    updateParams["custom:ipAddress"] = ipAddress;
                }

                if (!string.IsNullOrWhiteSpace(language) && AWSService.Instance.GetUserAttributeValue(user, "custom:language") != language)
                {
                    updateParams["custom:language"] = language;
                }

                if (!string.IsNullOrWhiteSpace(os) && AWSService.Instance.GetUserAttributeValue(user, "custom:os") != os)
                {
                    updateParams["custom:os"] = os;
                }

                await AWSService.Instance.UpdateUser(user.Username, updateParams, !user.Enabled);
                log.LogInformation($"User ${email} exists, {ipAddress}, {country}");

                if (autoSignInTokenValid)
                {
                    // check sign in token is in expired list
                    var currentTokens = AWSService.Instance.GetUserAttributeValue(user, "custom:tokens");
                    if (currentTokens?.Contains(signInToken) == true)
                    {
                        return CreateErrorResponse("sign_in_token attribute is expired");
                    }

                    AWSService.Instance.RemoveAttribute(user, "custom:tokens");
                    var passcode = await AWSService.Instance.RequestPasscode(email, language, appId: appId, disableEmail: true, linkType: linkType);
                    return new JsonResult(new { success = true, exist, user, passcode }) { StatusCode = StatusCodes.Status200OK };
                }

                if (requestPasscode)
                {
                    await AWSService.Instance.RequestPasscode(email, language, appId: appId, linkType: linkType);
                }

                AWSService.Instance.RemoveAttribute(user, "custom:tokens");
                return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
            }
            else
            {
                if (!email.EndsWith(Configurations.AzureB2C.EmailTestDomain))
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        // get from cognito first
                        name = AWSService.Instance.GetUserAttributeValue(user, "name");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            // get from LLM
                            name = await LLMService.Instance.GetNameFromEmail(email);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                var updateParams = new Dictionary<string, string>();
                                updateParams["name"] = name;
                                await AWSService.Instance.UpdateUser(user.Username, updateParams);
                            }
                        }
                    }

                    var group = await AWSService.Instance.GetUserGroup(user.Username);
                    var qrcodeUrl = await AnalyticsService.Instance.SubscribeNewUser(email, name, ipAddress, appId, language: language, country: country, os: os, role: group);
                    if(!string.IsNullOrWhiteSpace(qrcodeUrl))
                    {
                        var updateParams = new Dictionary<string, string>();
                        updateParams["custom:qrcode"] = qrcodeUrl;
                        await AWSService.Instance.UpdateUser(user.Username, updateParams);
                    }

                    // Send analytics tracking
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

            if (autoSignInTokenValid)
            {
                // check sign in token is in expired list
                var currentTokens = AWSService.Instance.GetUserAttributeValue(user, "custom:tokens");
                if (currentTokens?.Contains(signInToken) == true)
                {
                    return CreateErrorResponse("sign_in_token attribute is expired");
                }

                AWSService.Instance.RemoveAttribute(user, "custom:tokens");
                var passcode = await AWSService.Instance.RequestPasscode(email, language, appId: appId, disableEmail: true, linkType: linkType);
                return new JsonResult(new { success = true, exist, user, passcode }) { StatusCode = StatusCodes.Status200OK };
            }

            if (requestPasscode)
            {
                await AWSService.Instance.RequestPasscode(email, language, appId: appId, linkType: linkType);
            }

            // Success, return user info
            AWSService.Instance.RemoveAttribute(user, "custom:tokens");
            return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
        }
    }    
}
