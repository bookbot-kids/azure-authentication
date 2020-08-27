using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;

namespace Authentication.Tests
{
    /// <summary>
    /// TestUpdateRole Api
    /// </summary>
    public class UpdateRoleTest: BaseTest
    {
        public UpdateRoleTest(ITestOutputHelper output) : base(output) { }

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
            var response = (JsonResult)await UpdateRole.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }


        /// <summary>
        /// Test the invalid token. It should response error 400
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestTokenInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "duc@bookbotkids.com"},
                { "refresh_token", "123"}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await UpdateRole.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestSuccess()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "test@bookbotkids.com"},
                { "refresh_token", TestConfig.AdminToken}, // token of admin
                { "role", "subscriber"}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await UpdateRole.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            var isSuccess = response.Value.GetType().GetProperty("success")?.GetValue(response.Value, null) as bool?;
            Assert.True(isSuccess);
        }
    }
}
