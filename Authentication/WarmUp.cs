using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Library;
using System.Collections.Generic;

namespace Authentication
{
    /// <summary>
    /// A function to warm up the rest functions.
    /// Normally, functions with consumtion plan <see cref="https://docs.microsoft.com/en-us/azure/azure-functions/functions-scale#consumption-plan"/> can sleep after a while
    /// This function makes sure that all others functions are waked up before client call them
    /// </summary>
    public class WarmUp: BaseFunction
    {
        /// <summary>
        /// A http (Get, Post) method to warm up all other functions.<br/>
        /// All the warm up requests are executed in parallel at the same time and wait for all to complete
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"code": Azure function code for security</description></item>
        /// <item><description>"function_url": Base function url, where they are deployed</description></item>
        /// <item><description>"admin": true/false, whether to run admin functions or not</description></item>
        /// </list> 
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>User result with http code 200 if no error, otherwise return http error</returns>
        [FunctionName("WarmUp")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            // The function code should be available in azure function
            var code = req.Query["code"];
            if (string.IsNullOrWhiteSpace(code))
            {
                return CreateErrorResponse("code is missing");
            }

            // Read the base function url, uses to call other functions
            var url = req.Query["function_url"];
            if (string.IsNullOrWhiteSpace(url))
            {
                return CreateErrorResponse("function_url is missing");
            }

            var functions = new List<string>()
            {
                "CheckAccount",  "GetRefreshAndAccessToken", "GetUserInfo", "RefreshToken",  "GetResourceTokens"
            };

            // add admin functions to warm up
            bool includeAdminFunctions = req.Query["admin"] == "true";
            if (includeAdminFunctions)
            {
                functions.Add("CreateRolePermission");
                functions.Add("UpdateRole");
            }

            var tasks = new List<Task<string>>();

            // execute all requests in parallel
            foreach (var function in functions)
            {
                tasks.Add(ExecuteGetRequest($"{url}/{function}", new Dictionary<string, string>
                {
                    {"code", code },
                    {"source", "cognito" }
                }));
            }
            
            Task.WaitAll(tasks.ToArray());

            return CreateSuccessResponse();
        }
    }
}
