using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ExpireToken : BaseFunction
    {
        [FunctionName("ExpireToken")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            string email = req.Query["email"];
            email = email?.NormalizeEmail();
            string token = req.Query["token"];
            string refreshToken = req.Query["refresh_token"];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return CreateErrorResponse("missing refresh_token");
            }

            // cognito authentication
            var adToken = await AWSService.Instance.GetAccessToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
            }

            var user = await AWSService.Instance.FindUserByEmail(email, removeChallenge: false);
            if(user == null)
            {
                return CreateErrorResponse($"can not find user {email}", StatusCodes.Status401Unauthorized);
            }

            var attributeToken = AWSService.Instance.GetUserAttributeValue(user, "custom:tokens") ?? "";
            var allTokens = attributeToken.Split(";").ToList();
            if(allTokens.Count > 15)
            {
                allTokens.RemoveAt(0);
            }

            allTokens.Add(token);
            allTokens = allTokens.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            // update token attribute
            await AWSService.Instance.UpdateUser(user.Username, new Dictionary<string, string>
                {
                        {"custom:tokens", string.Join(";", allTokens) }
                });

            return CreateSuccessResponse();

        }
    }
}

