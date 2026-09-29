using System.Threading.Tasks;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Services;

namespace Authentication
{
    public class VerifyPasscode: BaseFunction
    {
        [Function("VerifyPasscode")]
        public Task<IActionResult> RunFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            FunctionContext executionContext) =>
            Run(req, executionContext.GetLogger<VerifyPasscode>());

        public async Task<IActionResult> Run(HttpRequest req, ILogger log)
        {
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

            email = email.NormalizeEmail();

            string passcode = req.Query["passcode"];
            if (string.IsNullOrWhiteSpace(passcode))
            {
                return CreateErrorResponse($"Passcode is empty");
            }

            var isValid = await AWSService.Instance.VerifyPasscode(email, passcode);
            if(!isValid)
            {
                return CreateErrorResponse($"Passcode {passcode} is invalid for email ${email}", statusCode: 401);
            }

            return CreateSuccessResponse();
        }
    }
}

