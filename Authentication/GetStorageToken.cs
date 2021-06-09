using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Services;
using Authentication.Shared.Library;
using Authentication.Shared.Models;

namespace Authentication
{
    public class GetStorageToken: BaseFunction
    {
        [FunctionName("GetStorageToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return CreateErrorResponse("refresh_token is missing", StatusCodes.Status403Forbidden);
            }

            // get access token by refresh token
            var adToken = await ADAccess.Instance.RefreshToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse("refresh_token is invalid", StatusCodes.Status401Unauthorized);
            }

            // Validate the access token
            var (result, message, _) = await ADAccess.Instance.ValidateAccessToken(adToken.AccessToken);
            if (!result)
            {
                return CreateErrorResponse(message, StatusCodes.Status403Forbidden);
            }

            log.LogInformation("GetStorageToken processed a request.");
            var uri = StorageService.Instance.CreateSASUri();
            return new JsonResult ( new { success = true, uri } );
        }
    }
}
