using System;
using System.Collections.Generic;
using System.Linq;
using Authentication.Shared.Models;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Utils
{
    /// <summary>
    /// Http helper util class
    /// </summary>
    public static class HttpHelper
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
        public static IActionResult CreateSuccessResponse()
        {
            return new JsonResult(new { success = true }) { StatusCode = StatusCodes.Status200OK };
        }

        /// <summary>
        /// Verify admin permission by access token
        /// </summary>
        /// <param name="authToken">Authencation token</param>
        /// <returns>IActionResult if it has error. Otherwise return null</returns>
        public static async System.Threading.Tasks.Task<IActionResult> VerifyAdminToken(string authToken)
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
        /// Generate password from email + secret.
        /// Need to add a prefix to by pass the password complexity requirements
        /// </summary>
        /// <param name="email">email adress</param>
        /// <returns>password hash</returns>
        public static string GeneratePassword(string email)
        {
            return Configurations.AzureB2C.PasswordPrefix + (email.ToLower() + Configurations.AzureB2C.PasswordSecretKey).MD5();
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
    }
}
