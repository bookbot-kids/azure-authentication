using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Abstractions;
namespace Authentication.Tests
{
    /// <summary>
    /// Test CreateRolePermission api
    /// </summary>
    public class CreateRolePermissionTest: BaseTest
    {
        public CreateRolePermissionTest(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test the invalid table
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestTableInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "table", "wrongTable"},
                { "auth_token", TestConfig.AdminToken}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CreateRolePermission.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the invalid role
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestRoleInvalid()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "role", "wrongRole"},
                { "auth_token", TestConfig.AdminToken}
            };
            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CreateRolePermission.Run(request, logger);
            Assert.Equal(400, response.StatusCode);
        }

        /// <summary>
        /// Test the success request to update table 
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestValidTable()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "table", "Books"},
                { "auth_token", TestConfig.AdminToken}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult) await CreateRolePermission.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            var isSuccess = response.Value.GetType().GetProperty("success")?.GetValue(response.Value, null) as bool?;
            Assert.True(isSuccess);
        }

        /// <summary>
        /// Test the success request to update role 
        /// </summary>
        /// <returns>Nothing</returns>
        [Fact]
        public async Task TestValidRole()
        {
            Dictionary<string, StringValues> parameters = new Dictionary<string, StringValues>()
            {
                { "role", "subscriber"},
                { "auth_token", TestConfig.AdminToken}
            };

            var request = CreateHttpRequest(parameters);
            var response = (JsonResult)await CreateRolePermission.Run(request, logger);
            Assert.Equal(200, response.StatusCode);
            var isSuccess = response.Value.GetType().GetProperty("success")?.GetValue(response.Value, null) as bool?;
            Assert.True(isSuccess);
        }
    }
}
