using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// <summary>
    /// Subscribe to sendy list
    /// </summary>
    public class SubscribeList : BaseFunction
    {
        [FunctionName("SubscribeList")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;
            var ipAddress = HttpHelper.GetIpFromRequestHeaders(req);
            string country = req.Query["country"];
            string email = req.Query["email"];
            email = email?.NormalizeEmail();
            string firstName = req.Query["first_name"];
            string lastName = req.Query["last_name"];
            string language = req.Query["language"];
            string os = req.Query["os"];
            string appId = req.Query["app_id"];
            var name = string.IsNullOrWhiteSpace(firstName) ?
                      (string.IsNullOrWhiteSpace(lastName) ? "" : lastName) :
                      (string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}");
            string subscribeList = req.Query["subscribe_list"];
            log.LogInformation($"Subcribe user {email}, {firstName} {lastName}");
            if (string.IsNullOrWhiteSpace(subscribeList))
            {
                return CreateErrorResponse("subscribe_list is invalid");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return CreateErrorResponse("Must provide first_name or last_name");
            }

            if(string.IsNullOrWhiteSpace(email))
            {
                return CreateErrorResponse("email is invalid");
            }

            var user = await AWSService.Instance.FindUserByEmail(email);
            if(user == null)
            {
                return CreateErrorResponse($"User $email is invalid");
            }

            // update user info
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

            await AWSService.Instance.UpdateUser(user.Username, updateParams, false);

            var group = await AWSService.Instance.GetUserGroup(user.Username);
            await AnalyticsService.Instance.SubscribeToEmailList(subscribeList, email, name, ipAddress, appId, language: language, country: country, os: os, role: group);
            return CreateSuccessResponse();
        }
    }
}

