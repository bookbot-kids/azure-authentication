using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Authentication.Tests
{
    public class GetRefreshAndAccessTokenTest : BaseTest
    {
        public GetRefreshAndAccessTokenTest(ITestOutputHelper output) : base(output) { }


        /// <summary>
        /// Test the id token is invalid. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestIdTokenInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "id_token", "123"}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetRefreshAndAccessToken.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the id is missing. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestIdTokenMissing()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>();
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetRefreshAndAccessToken.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful with valid id token
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestValidIdToken()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "id_token", "valid_id_token"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetRefreshAndAccessToken.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Value);
        }

        /// <summary>
        /// Test the api with invalid email
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestInvalidLogin()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "fake@bookbotkids.com"},
                { "password", "valid_password"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetRefreshAndAccessToken.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful with valid email and password
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestValidLogin()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "duc@bookbotkids.com"},
                { "password", "valid_password"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetRefreshAndAccessToken.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Value);
        }
    }
}
