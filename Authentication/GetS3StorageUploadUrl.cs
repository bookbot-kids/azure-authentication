using Authentication.Shared;
using Authentication.Shared.Library;
using Authentication.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class GetS3StorageUploadUrl : BaseFunction
    {
        [Function("GetS3StorageUploadUrl")]
        public static IActionResult RunFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            FunctionContext executionContext) =>
            Run(req, executionContext.GetLogger<GetS3StorageUploadUrl>());

        public static IActionResult Run(HttpRequest req, ILogger log)
        {
            log.LogInformation("GetS3StorageUploadUrl processed a request.");

            // Parse the request body
            var uploadPath = req.Query["upload_path"];
            var forDownload = req.Query["for_download"] == "true";

            if (string.IsNullOrEmpty(uploadPath))
            {
                return CreateErrorResponse("Please provide a file and an upload path", StatusCodes.Status400BadRequest);
            }

            if (!uploadPath.ToString().StartsWith(Configurations.Cognito.AWSS3MainPrefixPath))
            {
                return CreateErrorResponse("Invalid path", StatusCodes.Status400BadRequest);
            }

            // Generate pre-signed URL
            var presignedUrl = AWSService.Instance.GeneratePreSignedURL(Configurations.Cognito.AWSS3MainBucket, uploadPath, forDownload);
            return new JsonResult(new { success = true, url = presignedUrl }) { StatusCode = StatusCodes.Status200OK };
        }
    }

}

