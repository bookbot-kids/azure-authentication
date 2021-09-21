using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace Authentication.Shared.Library
{
    public static class HttpHelper
    {
        /// <summary>
        /// Parse to json object from http response
        /// </summary>
        /// <param name="httpResponse">http response</param>
        /// <returns>Json object</returns>
        public static async Task<JObject> GetJson(HttpResponseMessage httpResponse)
        {
            var data = await httpResponse.Content.ReadAsStringAsync();
            return JObject.Parse(data);
        }

        public static async Task<dynamic> DeserializeJson(HttpResponseMessage httpResponse)
        {
            var data = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(data);
        }

        public static string GetIpFromRequestHeaders(HttpRequest request)
        {
            return (request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "").Split(new char[] { ':' }).FirstOrDefault();
        }

        public static async Task TimeoutAfter(Task task, TimeSpan timeout)
        {

            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
    }
}
