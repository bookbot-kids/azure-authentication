using System;
using System.Net.Http;
using System.Threading;
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
        private static readonly HttpClient httpClient = new HttpClient();
        private const int MaxRetries = 3;
        private static readonly TimeSpan UploadTimeout = TimeSpan.FromMinutes(5);

        [FunctionName("S3StorageUpload")]
        public static async Task<IActionResult> Run(
          [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            log.LogInformation("S3StorageUpload processed a request.");

            try
            {
                // Parse the request body
                var file = req.Form.Files["file"];
                var uploadPath = req.Form["upload_path"];

                if (file == null || string.IsNullOrEmpty(uploadPath))
                {
                    return CreateErrorResponse("Please provide a file and an upload path", StatusCodes.Status400BadRequest);
                }

                if (!uploadPath.ToString().StartsWith(Configurations.Cognito.AWSS3MainPrefixPath))
                {
                    return CreateErrorResponse("Invalid path", StatusCodes.Status400BadRequest);
                }

                // Generate pre-signed URL
                var presignedUrl = AWSService.Instance.GeneratePreSignedURL(Configurations.Cognito.AWSS3MainBucket, uploadPath);

                // Implement retry logic
                for (int i = 0; i < MaxRetries; i++)
                {
                    try
                    {
                        // Upload the file to S3 using pre-signed URL
                        using (var stream = file.OpenReadStream())
                        {
                            var content = new StreamContent(stream);
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                            using (var cts = new CancellationTokenSource(UploadTimeout))
                            {
                                var response = await httpClient.PutAsync(presignedUrl, content, cts.Token);
                                response.EnsureSuccessStatusCode();
                            }
                        }

                        return new JsonResult(new { success = true, message = "Uploaded successfully" });
                    }
                    catch (OperationCanceledException)
                    {
                        log.LogWarning($"Upload operation timed out. Attempt {i + 1} of {MaxRetries}");
                        if (i == MaxRetries - 1)
                        {
                            return CreateErrorResponse("Upload operation timed out", StatusCodes.Status408RequestTimeout);
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        log.LogWarning($"Upload failed from client. Attempt {i + 1} of {MaxRetries}");
                        if (i == MaxRetries - 1)
                        {
                            return CreateErrorResponse("Upload failed from client", StatusCodes.Status417ExpectationFailed);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        log.LogWarning($"HTTP request failed. Attempt {i + 1} of {MaxRetries}. Error: {ex.Message}");
                        if (i == MaxRetries - 1)
                        {
                            // Determine if it's a client error (4xx) or server error (5xx)
                            if (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 400 && (int)ex.StatusCode.Value < 500)
                            {
                                return CreateErrorResponse($"Client error: {ex.Message}", StatusCodes.Status417ExpectationFailed);
                            }
                            else
                            {
                                throw; // Will be caught by the outer catch block and return 500
                            }
                        }
                    }
                }

                // If we get here, all retries failed
                throw new Exception("All upload attempts failed");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error uploading file to S3.");
                return CreateErrorResponse($"An error occurred while uploading the file {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }

}

