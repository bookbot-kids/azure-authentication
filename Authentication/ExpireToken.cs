using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class ExpireToken : BaseFunction
    {
        [Function("ExpireToken")]
        public Task<IActionResult> RunFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext executionContext) =>
            Run(req, executionContext.GetLogger<ExpireToken>());

        public async Task<IActionResult> Run(HttpRequest req, ILogger log)
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
            ADToken adToken;
            try
            {
                adToken = await AWSService.Instance.GetAccessToken(refreshToken);
            }
            catch (CognitoUnavailableException ex)
            {
                log.LogError(ex, "ExpireToken: Cognito did not answer the token request (" + ex.InnerException?.GetType().Name + ": " + ex.InnerException?.Message + "), answering 503 instead of 401");
                // Cognito gave no answer, so the refresh token was not rejected: never 401 here,
                // the apps sign the user out and wipe local data on 401
                return CreateErrorResponse("sign-in service is temporarily unavailable, please retry", StatusCodes.Status503ServiceUnavailable);
            }
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse("refresh_token is invalid", StatusCodes.Status401Unauthorized);
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

