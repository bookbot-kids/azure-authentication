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

        /// <summary>
        /// Executes the specified function with retry logic
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="operation">The operation to execute</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <param name="retryDelayMilliseconds">Initial delay between retries in milliseconds</param>
        /// <param name="useExponentialBackoff">Whether to use exponential backoff for delays</param>
        /// <param name="onRetry">Optional action to execute before each retry</param>
        /// <returns>The result of the operation</returns>
        public static async Task<T> ExecuteWithRetryAsync<T>(
            Func<Task<T>> operation,
            int maxRetries = 3,
            int retryDelayMilliseconds = 1000,
            bool useExponentialBackoff = true,
            Func<Exception, int, Task> onRetry = null, string comment = "")
        {
            int currentAttempt = 0;

            while (true)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    currentAttempt++;

                    if (currentAttempt >= maxRetries)
                    {
                        throw new Exception($"Operation {comment} failed after {maxRetries} attempts", ex);
                    }

                    // Calculate delay for this attempt
                    int delay = useExponentialBackoff
                        ? retryDelayMilliseconds * (int)Math.Pow(2, currentAttempt - 1)
                        : retryDelayMilliseconds;

                    // Execute the onRetry callback if provided
                    if (onRetry != null)
                    {
                        await onRetry(ex, delay);
                    }

                    await Task.Delay(delay);
                }
            }
        }

        /// <summary>
        /// Non-generic version for operations that don't return a value
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            int maxRetries = 3,
            int retryDelayMilliseconds = 1000,
            bool useExponentialBackoff = true,
            Func<Exception, int, Task> onRetry = null, string comment = "")
        {
            await ExecuteWithRetryAsync<object>(
                async () =>
                {
                    await operation();
                    return null;
                },
                maxRetries,
                retryDelayMilliseconds,
                useExponentialBackoff,
                onRetry, comment);
        }
    }
}
