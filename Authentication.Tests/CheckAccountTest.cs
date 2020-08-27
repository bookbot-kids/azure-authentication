using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Authentication.Tests
{
    /// <summary>
    /// Test the CheckAccount Api
    /// </summary>
    public class CheckAccountTest : BaseTest
    {
        public CheckAccountTest(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test the invalid email. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestEmailInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "invalidEmail@@@.com"}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the email is missing in request. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestEmailMissing()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>();
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful with valid email
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestSuccess()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "jade@bookbotkids.com"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
        }
    }
}
