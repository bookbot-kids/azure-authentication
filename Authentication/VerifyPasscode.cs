using System.Threading.Tasks;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Services;

namespace Authentication
{
    public class VerifyPasscode: BaseFunction
    {
        [FunctionName("VerifyPasscode")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
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

            var isValid = await CognitoService.Instance.VerifyPasscode(email, passcode);
            if(!isValid)
            {
                return CreateErrorResponse($"Passcode {passcode} is invalid for email ${email}", statusCode: 401);
            }

            return CreateSuccessResponse();
        }
    }
}

