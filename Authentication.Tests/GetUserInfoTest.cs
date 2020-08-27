using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;


namespace Authentication.Tests
{
    public class GetUserInfoTest : BaseTest
    {
        public GetUserInfoTest(ITestOutputHelper output) : base(output) { }


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
            var response = (JsonResult)await GetUserInfo.Run(request, logger);
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
            var response = (JsonResult)await GetUserInfo.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the api successful with existing email account
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestExistingUser()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", "duc@bookbotkids.com"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await GetUserInfo.Run(request, logger);
            Assert.Equal(200, response.StatusCode);

            var isSuccess = response.Value.GetType().GetProperty("success")?.GetValue(response.Value, null) as bool?;
            Assert.True(isSuccess);
            var user = response.Value.GetType().GetProperty("user")?.GetValue(response.Value, null);
            Assert.NotNull(user);
        }

        /// <summary>
        /// Test the api successful with new email account 
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestNewUser()
        {
            var randomId = Guid.NewGuid().ToString();
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "email", $"{randomId}@bookbotkids.com"}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CheckAccount.Run(request, logger);
            Assert.Equal(200, response.StatusCode);

            var isSuccess = response.Value.GetType().GetProperty("success")?.GetValue(response.Value, null) as bool?;
            Assert.True(isSuccess);
            var user = response.Value.GetType().GetProperty("user")?.GetValue(response.Value, null);
            Assert.Null(user);
        }
    }
}
