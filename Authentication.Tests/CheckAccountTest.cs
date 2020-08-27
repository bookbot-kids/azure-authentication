using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Authentication.Tests
{
    public class CheckAccountTest
    {
        private readonly ILogger logger = TestFactory.CreateLogger();

        [Fact]
        public async Task TestEmailInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>();
            var request = TestFactory.CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task TestSuccess()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "test@gmail.com"}
            };

            var request = TestFactory.CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
        }
    }
}
