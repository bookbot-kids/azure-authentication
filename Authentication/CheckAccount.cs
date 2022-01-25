using System.Threading.Tasks;
using Authentication.Shared.Models;
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

            // Fix the encode issue because email parameter that contains "+" will be encoded by space
            // e.g. client sends "a+1@gmail.com" => Azure function read: "a 1@gmail.com" (= req.Query["email"])
            // We need to replace space by "+" when reading the parameter req.Query["email"]
            // Then the result is correct "a+1@gmail.com"
            email = email.Trim().Replace(" ", "+");

            string name = email.GetNameFromEmail();

            log.LogInformation($"Check account for user ${email}");

            // check if email is existed in b2c. If it is, return that user
            var (exist, user) = await ADUser.FindOrCreate(email, name, country, ipAddress);

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
                    updateParams["country"] = country;
                    user.Country = country;
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    updateParams["streetAddress"] = ipAddress;
                    user.IPAddress = ipAddress;
                }

                if (updateParams.Count > 0)
                {
                    await user.Update(updateParams);
                }

                log.LogInformation($"User ${email} exists, {ipAddress}, {country}");

                return new JsonResult(new { success = true, exist, user }) { StatusCode = StatusCodes.Status200OK };
            }
            else
            {
                log.LogInformation($"User ${email} not exists, try to create a new one with {ipAddress}, {country}");
                try
                {
                    await SendAnalytics(email, user.ObjectId, country, ipAddress, name);
                }
                catch (Exception ex)
                {
                    if (!(ex is TimeoutException))
                    {
                        log.LogError($"Send analytics error {ex.Message}");
                    }                    
                }
            }

            // add user to new group
            var newGroup = await ADGroup.FindByName("new");
            var addResult = await newGroup.AddUser(user.ObjectId);

            // there is an error when add user into new group
            if (!addResult)
            {
                return CreateErrorResponse($"can not add user {email} into new group", StatusCodes.Status500InternalServerError);
            }

            // Success, return user info
            return new JsonResult(new { success = true, exist, user}) { StatusCode = StatusCodes.Status200OK };
        }

        private async Task SendAnalytics(string email, string id, string country, string ipAddress, string name)
        {
            // log event for new user
            var body = new Dictionary<string, object>
                {
                    {
                        "googleAnalytics", new Dictionary<string, string>
                            {
                                {"p_country", country },
                                {"p_email", email },
                                {"uid", id },
                                {"eventType", "event" },
                                {"eventName", "sign_up"},
                                {"measurement_id", Configurations.Configuration["GAMeasurementId"]},
                                {"api_secret", Configurations.Configuration["GASecret"]},
                                {"p_role", "new" }
                            }
                    },
                    {
                         "facebookPixel", new Dictionary<string, string>
                            {
                                {"em", email },
                                {"country", country },
                                {"client_ip_address",  ipAddress},
                                {"hashFields", "em,country" },
                                {"eventType", "event" },
                                {"eventName", "CompleteRegistration"}
                            }
                    },
                    {
                        "activeCampaign", new Dictionary<string, string>
                            {
                                {"country", country },
                                {"eventType", "user" },
                                {"firstName", name },
                                {"email", email },
                                {"role", "new" }
                            }
                    },
                };

            // don't need to wait for this event, just make it timeout after few seconds
            var task = AnalyticsService.Instance.SendEvent(body);
            await HttpHelper.TimeoutAfter(task, TimeSpan.FromSeconds(5));
        }
    }    
}
