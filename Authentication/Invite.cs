using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using Authentication.Shared;
using Authentication.Shared.Library;
using Authentication.Shared.Services;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class Invite : BaseFunction
    {
        [FunctionName("Invite")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            string email = req.Query["email"];
            email = email?.NormalizeEmail();
            string language = req.Query["language"];
            string country = req.Query["country"];
            string phone = req.Query["phone"];
            phone = phone?.NormalizePhone();
            string name = email?.GetNameFromEmail();
            var ipAddress = HttpHelper.GetIpFromRequestHeaders(req);
            Dictionary<string, string> bodyData;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                bodyData = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
            }
            catch(Exception)
            {
                return CreateErrorResponse($"Body data is not valid");
            }

            string androidPackageName = req.Query["androidPackageName"];
            string iosBundleId = req.Query["iosBundleId"];
            string iosAppStoreId = req.Query["iosAppStoreId"];
            string domainUrl = req.Query["domainUrl"];

            if (string.IsNullOrWhiteSpace(androidPackageName) || string.IsNullOrWhiteSpace(iosBundleId)
                || string.IsNullOrWhiteSpace(iosAppStoreId) || string.IsNullOrWhiteSpace(domainUrl)
                || string.IsNullOrWhiteSpace(language))
            {
                return CreateErrorResponse($"Missing parameters");
            }


            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                return CreateErrorResponse($"Email or phone should not empty");
            }

            var userEmail = "";
            UserType user = null;

            // whatsapp
            if (!string.IsNullOrWhiteSpace(phone))
            {
                // get or create user by phone
                user = await CognitoService.Instance.FindUserByPhone(phone);
                var placeholderEmail = phone + Configurations.Whatsapp.PlaceholderEmail;
                userEmail = string.IsNullOrWhiteSpace(email) ? placeholderEmail : email;
                if (user == null)
                {
                    // if user with phone not exist, then create a new one with place holder email
                    var (exist, newUser) = await CognitoService.Instance.FindOrCreateUser(userEmail, name, country, ipAddress, phone: phone, forceCreate: string.IsNullOrWhiteSpace(email));
                    user = newUser;

                    if (exist)
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
                } else
                {
                    var accountEmail = CognitoService.Instance.GetUserAttributeValue(user, "email");
                    userEmail = accountEmail ?? userEmail;
                }

                
            }
            // cognito
            else if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.IsValidEmailAddress())
                {
                    return CreateErrorResponse($"Email {email} is invalid");
                }

                var (exist, existUser) = await CognitoService.Instance.FindOrCreateUser(email, name, country, ipAddress);
                // there is an error when creating user
                if (existUser == null)
                {
                    return CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
                }

                user = existUser;

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
            }

            var userId = CognitoService.Instance.GetUserAttributeValue(user, "preferred_username");

            // generate deeplink
            var parameters = new Dictionary<string, string> {
                    {"id", userId },
                    {"email", userEmail },
                };

            if(!string.IsNullOrWhiteSpace(phone))
            {
                parameters["phone"] = phone;
                parameters["type"] = "whatsapp";
            }
            else
            {
                parameters["type"] = "email";
            }

            foreach (var kvp in bodyData)
            {
                parameters.Add(kvp.Key, kvp.Value);
            }

            var expired = ((DateTimeOffset)DateTime.Now.AddDays(7)).ToUnixTimeMilliseconds();
            parameters["expired"] = expired.ToString();

            // get passcode
            var passcode = await CognitoService.Instance.RequestPasscode(userEmail, language, disableEmail: true);
            parameters["passcode"] = passcode;

            var link = await GoogleService.Instance.GenerateDynamicLink(Configurations.DeepLink.Key, domainUrl, androidPackageName,
                iosBundleId, iosAppStoreId, parameters
                );

            return new JsonResult(new { success = true, link = link.ShortLink }) { StatusCode = StatusCodes.Status200OK };
        }
    }
}

