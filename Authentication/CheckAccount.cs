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
using QRCoder;
using System.IO;

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
            string linkType = req.Query["link_type"];
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
                        return await ProcessCognitoRequest(log, email, name, country, ipAddress, requestPasscode, language, appId, signInToken, autoSignInTokenValid, linkType);
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
                        return await ProcessWhatsappRequest(log, phone, email, name, country, ipAddress, language, appId, signInToken, autoSignInTokenValid, linkType);
                    }
                default:
                    throw new Exception($"{email} has invalid source {source}");
            }
        }

        private async Task<IActionResult> ProcessWhatsappRequest(ILogger log, string phone, string email, string name, string country, string ipAddress, string language, string appId, string signInToken, bool autoSignInTokenValid, string linkType)
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

        private async Task<IActionResult> ProcessCognitoRequest(ILogger log, string email, string name, string country, string ipAddress, bool requestPasscode, string language, string appId, string signInToken, bool autoSignInTokenValid, string linkType)
        {
            var (exist, user) = await AWSService.Instance.FindOrCreateUser(email, name, country, ipAddress);
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
                if (!string.IsNullOrWhiteSpace(country) && AWSService.Instance.GetUserAttributeValue(user, "custom:country") != country)
                {
                    updateParams["custom:country"] = country;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress) && AWSService.Instance.GetUserAttributeValue(user, "custom:ipAddress") != ipAddress)
                {
                    updateParams["custom:ipAddress"] = ipAddress;
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
                    await SubscribeToEmailList(log, email, name, ipAddress, appId);

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

        private static async Task SubscribeToEmailList(ILogger log, string email, string name, string ipAddress, string appId)
        {
            // create deeplink and add to sendy
            var expiry = DateTime.Now.AddDays(35).ToString("yyyy-MM-dd HH:mm:ss.fff");
            var branchIOKey = Configurations.Configuration[$"BranchIOKey_{appId}"];
            if (!string.IsNullOrWhiteSpace(branchIOKey))
            {
                var data = new Dictionary<string, object>
                        {
                             {"branch_key", branchIOKey},
                             {"channel", Configurations.Apple.AppleServiceId},
                             {"feature", "email"},
                             {"campaign", "offer"},
                             {"tags", new List<string> { "Discount" } },
                             {"data", new Dictionary<string, object> {
                                 {"offer", Configurations.Configuration["OfferPackage"]},
                                 {"$canonical_url", Configurations.JWTToken.TokenIssuer},
                                 {"expiry", expiry},

                             } },
                        };

                var deepLink = await AnalyticsService.Instance.CreateDeepLink(data);

                // create QRCode image
                var filename = Guid.NewGuid().ToString() + ".png";
                var qrcodeUrl = $"{Configurations.Configuration["CloudFlareR2Url"]}/qrcode/{filename}";

                log.LogInformation($"Generate link {deepLink}, qrcode url {qrcodeUrl} for user ${email}");
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(10);
                    // upload qrcode to cloudflare r2
                    await StorageService.UpdateToCloudFlareR2(Configurations.Configuration["CloudFlareR2Key"], qrcodeUrl, qrCodeImage, "image/png");

                    // Send to sendy
                    await AnalyticsService.Instance.SubscribeToSendyList(Configurations.Analytics.SendyRegisteredListId, email, name: name, ipAddress: ipAddress, offer: deepLink, qrcode: qrcodeUrl);
                }
            }
            else
            {
                log.LogError($"Ignore invalid appid {appId}");
            }
        }
    }    
}
