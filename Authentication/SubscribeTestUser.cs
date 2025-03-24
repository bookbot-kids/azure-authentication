using System.Threading.Tasks;
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
    public class SubscribeTestUser : BaseFunction
    {
        [FunctionName("SubscribeTestUser")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string clientToken = req.Query["client_token"];
            if (string.IsNullOrWhiteSpace(clientToken))
            {
                return CreateErrorResponse("client_token is missing", StatusCodes.Status401Unauthorized);
            }
            var (validateResult, clientTokenMessage, payload) = TokenService.ValidateClientToken(clientToken, Configurations.JWTToken.TokenClientSecret,
                 Configurations.JWTToken.TokenIssuer, Configurations.JWTToken.TokenSubject);
            if (!validateResult)
            {
                return CreateErrorResponse(clientTokenMessage, StatusCodes.Status401Unauthorized);
            }

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

            // only subscribe user from special domain
            if (!StringHelper.IsTestEmail(Configurations.AzureB2C.EmailTestDomain, email))
            {
                return CreateErrorResponse($"email {email} is invalid");
            }

            email = email.NormalizeEmail();
            var user = await AWSService.Instance.FindUserByEmail(email);
            if (user == null)
            {
                return CreateErrorResponse($"email {email} is invalid");
            }

            var userId = user.Username;
            log.LogInformation($"Subscribe user {userId}, {email}");
            await AWSService.Instance.UpdateUserGroup(userId, "subscriber");
            return CreateSuccessResponse();
        }
    }
}

