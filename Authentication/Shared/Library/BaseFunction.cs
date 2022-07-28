using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Library
{
    /// <summary>
    /// Base function class, has many util methods
    /// </summary>
    public abstract class BaseFunction
    {
        /// <summary>
        /// Create error json response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="statusCode">Http status code</param>
        /// <returns>Error response</returns>
        public static IActionResult CreateErrorResponse(string message, int statusCode = StatusCodes.Status400BadRequest)
        {
            // log error
            var stackTrace = Environment.StackTrace;
            Logger.Log?.LogError($"Response error {statusCode}: {message} at stacktrace {stackTrace}");
            return new JsonResult(new { success = false, error = message }) { StatusCode = statusCode };
        }

        /// <summary>
        /// Create success json response
        /// </summary>
        /// <returns>Success response</returns>
        public static IActionResult CreateSuccessResponse(object body = null)
        {
            return new JsonResult(body ?? new { success = true }) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Verify admin permission by access token
        /// </summary>
        /// <param name="authToken">Authencation token</param>
        /// <returns>IActionResult if it has error. Otherwise return null</returns>
        public static async Task<IActionResult> VerifyAdminToken(string authToken)
        {
            // validate auth token
            if (string.IsNullOrWhiteSpace(authToken))
            {
                return CreateErrorResponse("auth_token is missing", StatusCodes.Status401Unauthorized);
            }

            var (result, message, id) = await ADAccess.Instance.ValidateAccessToken(authToken);
            if (!result || string.IsNullOrWhiteSpace(id))
            {
                return CreateErrorResponse("auth_token is invalid", StatusCodes.Status401Unauthorized);
            }

            var user = await ADUser.FindById(id);

            // make sure user is in admin group
            var adminGroup = await ADGroup.FindByName(Configurations.AzureB2C.AdminGroup);
            var isMemberOf = await adminGroup.HasUser(user.ObjectId);
            if (!isMemberOf)
            {
                return CreateErrorResponse("Insufficient privileges", StatusCodes.Status401Unauthorized);
            }

            return null;
        }       

        /// <summary>
        /// Get bearer authorization
        /// </summary>
        /// <param name="token">access token</param>
        /// <returns>Bearer authorization</returns>
        public static string GetBearerAuthorization(string token)
        {
            return Configurations.AzureB2C.BearerAuthentication + token;
        }

        /// <summary>
        /// Get id address from client request
        /// </summary>
        /// <param name="request">the request</param>
        /// <returns>ip address</returns>
        public static string GetIpFromRequestHeaders(HttpRequest request)
        {
            var headers = request.Headers["X-Forwarded-For"];
            if (headers.Count > 0)
            {
                return headers.FirstOrDefault().Split(new char[] { ',' }).FirstOrDefault().Split(new char[] { ':' }).FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Run simple get request
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="parameters">Get parameters</param>
        /// <returns>String response or null</returns>
        public static async Task<string> ExecuteGetRequest(string url, Dictionary<string, string> parameters)
        {
            string query;
            try
            {
                using (var content = new FormUrlEncodedContent(parameters))
                {
                    query = content.ReadAsStringAsync().Result;
                }

                var client = new HttpClient
                {
                    BaseAddress = new Uri(url)
                };

                HttpResponseMessage response = await client.GetAsync($"?{query}");
                return response.Content.ReadAsStringAsync().Result;
            }catch(Exception)
            {
                return null;
            }
        }
    }
}
