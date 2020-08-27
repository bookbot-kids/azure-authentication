using System.Collections.Generic;
using System.IO;
using Authentication.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit.Abstractions;

namespace Authentication.Tests
{
    /// <summary>
    /// Base test class, uses to setup test environment. Ref <see cref="https://docs.microsoft.com/en-us/azure/azure-functions/functions-test-a-function"/>
    /// </summary>
    public class BaseTest
    {
        /// <summary>
        /// Console output
        /// </summary>
        protected readonly ITestOutputHelper output;

        /// <summary>
        /// Logger instance
        /// </summary>
        protected readonly ILogger logger = CreateLogger();

        /// <summary>
        ///  Initialization all test resources with the output is injected by DI
        /// </summary>
        /// <param name="output">Console output</param>
        public BaseTest(ITestOutputHelper output)
        {
            this.output = output;
            SetupEnvironment();
        }

        /// <summary>
        /// Do release resources
        /// </summary>
        public void Dispose()
        {
            
        }

        /// <summary>
        /// Setup needed resources
        /// </summary>
        private void SetupEnvironment()
        {
            var configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("local.settings.json", true, true)
             .Build();
            Configurations.Configuration = configuration;
        }

        /// <summary>
        /// Create logger instance
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;

            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }

            return logger;
        }

        /// <summary>
        /// Create test http request
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static HttpRequest CreateHttpRequest(Dictionary<string, StringValues> parameters)
        {
            var context = new DefaultHttpContext();
            var request = context.Request;
            request.Query = new QueryCollection(parameters);
            return request;
        }
    }
}
