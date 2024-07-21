using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Services;
using Authentication.Shared.Library;
using Authentication.Shared;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Collections.Generic;

namespace Authentication
{
    public class GetStorageToken: BaseFunction
    {
        [FunctionName("GetStorageToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return CreateErrorResponse("refresh_token is missing", StatusCodes.Status403Forbidden);
            }

            // cognito authentication
            var adToken = await AWSService.Instance.GetAccessToken(refreshToken);
            if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
            }

            log.LogInformation("GetStorageToken processed a request.");
            var storageService = new StorageService(Configurations.Storage.MainStorageConnection);
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var paths = data.paths;
            var uris = new List<Uri> { };
            foreach (string item in paths)
            {
                string container = item.Split("/")[0];
                string subPath = item.Remove(0, container.Length + 1);
                var uri = storageService.CreateFileSASUriAsync(container, subPath);
                uris.Add(uri);
            }

            return new JsonResult ( new { success = true, path = uris } );
        }
    }
}
