using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Authentication.Tests
{
    public class RefreshTokenTest: BaseTest
    {
        public RefreshTokenTest(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test the refresh token is invalid. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestRefreshTokenInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "refresh_token", "123"}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await RefreshToken.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the token is missing. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestRefreshTokenMissing()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>();
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await RefreshToken.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful with valid refresh token
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestValidToken()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "refresh_token", "valid_refresh_token"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await RefreshToken.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            Assert.NotNull(response.Value);
        }
    }
}
