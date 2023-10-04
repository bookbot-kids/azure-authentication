using System;
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
    public class ExpirePasscode : BaseFunction
    {
        [FunctionName("ExpirePasscode")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Set the logger instance
            Logger.Log = log;

            string email = req.Query["email"];
            email = email?.NormalizeEmail();
            string passcode = req.Query["passcode"];
            string refreshToken = req.Query["refresh_token"];

            // cognito authentication
            var adToken = await CognitoService.Instance.GetAccessToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
            }

            var user = await CognitoService.Instance.FindUserByEmail(email, removeChallenge: false);
            if(user == null)
            {
                return CreateErrorResponse($"can not find user {email}", StatusCodes.Status401Unauthorized);
            }

            var attributePasscode = CognitoService.Instance.GetUserAttributeValue(user, "custom:authChallenge");
            if(string.IsNullOrWhiteSpace(attributePasscode))
            {
                return CreateErrorResponse($"can not get passcode");
            }

            var passcodes = attributePasscode.Split(";");
            var newPasscodes = new List<string>();
            var hasFound = false;
            foreach (var token in passcodes)
            {
                var parts = token.Split(",");
                if (parts[0] == passcode)
                {
                    // make passcode expired
                    var expired = ((DateTimeOffset)DateTime.Now.AddDays(-1)).ToUnixTimeMilliseconds();
                    newPasscodes.Add($"{parts[0]},{expired}");
                    hasFound = true;
                }
                else
                {
                    newPasscodes.Add(token);
                }                
            }

            if(hasFound)
            {
                // update passcode attribute
                await CognitoService.Instance.UpdateUser(user.Username, new Dictionary<string, string>
                {
                    {"custom:authChallenge", string.Join(";", newPasscodes) }
                });
                return CreateSuccessResponse();
            }

            return CreateErrorResponse("Not found passcode");
        }
    }
}

