using System;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Library;
using Authentication.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    public class S3StorageUpload : BaseFunction
    {
        [FunctionName("S3StorageUpload")]
        public static async Task<IActionResult> Run(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            log.LogInformation("S3StorageUpload processed a request.");

            // Parse the request body
            var file = req.Form.Files["file"];
            var uploadPath = req.Form["upload_path"];

            if (file == null || string.IsNullOrEmpty(uploadPath))
            {
                return CreateErrorResponse("Please provide a file and an upload path", StatusCodes.Status400BadRequest);
            }

            if(!uploadPath.ToString().StartsWith(Configurations.Cognito.AWSS3MainPrefixPath))
            {
                return CreateErrorResponse("Invalid path", StatusCodes.Status400BadRequest);
            }

            try
            {
                // Generate pre-signed URL
                var presignedUrl = AWSService.Instance.GeneratePreSignedURL(Configurations.Cognito.AWSS3MainBucket, uploadPath);

                // Upload the file to S3 using pre-signed URL
                using (var httpClient = new HttpClient())
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var content = new StreamContent(stream);
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                        var response = await httpClient.PutAsync(presignedUrl, content);
                        response.EnsureSuccessStatusCode();
                    }
                }

                return new JsonResult(new { success = true, message = "Uploaded successfully" });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error uploading file to S3.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

