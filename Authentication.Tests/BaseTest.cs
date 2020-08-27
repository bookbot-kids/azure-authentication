using System;
using System.Collections.Generic;
using System.IO;
using Authentication.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
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
            // read config file
            var dir = Directory.GetCurrentDirectory();
            var settings = JsonConvert.DeserializeObject<LocalSettings>(File.ReadAllText(dir + "/local.settings.json"));

            // then write into system variables
            foreach (var setting in settings.Values)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }

            // and load configuration from environment variables
            var configuration = new ConfigurationBuilder()
             .SetBasePath(dir)
             .AddJsonFile("local.settings.json", true, true)
             .AddEnvironmentVariables()
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

        /// <summary>
        /// The local json settings parsed from local.settings.json file
        /// </summary>
        class LocalSettings
        {
            /// <summary>
            /// Is encrypted
            /// </summary>
            public bool IsEncrypted { get; set; }

            /// <summary>
            /// Config values
            /// </summary>
            public Dictionary<string, string> Values { get; set; }
        }
    }
}
