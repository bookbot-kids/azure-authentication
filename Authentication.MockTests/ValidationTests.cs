using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;
using static Authentication.MockTests.TestSetup;

namespace Authentication.MockTests
{
    /// <summary>
    /// Runs every function with input it must reject before calling any live service, and pins the
    /// exact response. These match production's responses captured before the isolated migration.
    /// GetResourceTokens has no validation-only path (it serves guests without a token), so it is
    /// covered by wire-format tests until dependencies can be mocked.
    /// </summary>
    public class ValidationTests
    {
        static readonly NullLogger log = NullLogger.Instance;

        public ValidationTests() => UseTestConfiguration();

        static void AssertRejected(IActionResult result, int status, string error)
        {
            var (code, body) = Read(result);
            Assert.Equal(status, code);
            Assert.True(JToken.DeepEquals(new JObject { ["success"] = false, ["error"] = error }, body), body.ToString());
        }

        static Task<IActionResult> Sync(IActionResult result) => Task.FromResult(result);

        [Theory]
        [InlineData("sign_in_token=x", "user_id is required with sign_in_token")]
        [InlineData("sign_in_token=!!not-base64!!&user_id=0123456789abcdef-user", "sign_in_token is not valid")]
        public async Task CheckAccount(string query, string error) =>
            AssertRejected(await new CheckAccount().Run(Request(query), log), 400, error);

        [Fact]
        public async Task CreateRolePermission() =>
            AssertRejected(await Authentication.CreateRolePermission.Run(Request(), log), 401, "auth_token is missing");

        [Fact]
        public async Task DeleteUser() =>
            AssertRejected(await new DeleteUser().Run(Request(method: "POST"), log), 401, "client_token is missing");

        [Fact]
        public async Task ExpireToken() =>
            AssertRejected(await new ExpireToken().Run(Request(method: "POST"), log), 400, "missing refresh_token");

        [Theory]
        [InlineData("")]
        [InlineData("email=someone%40example.com")]
        public async Task GetRefreshAndAccessToken(string query) =>
            AssertRejected(await new GetRefreshAndAccessToken().Run(Request(query), log), 400, "email & password are required");

        [Fact]
        public async Task GetS3StorageUploadUrl() =>
            AssertRejected(await Sync(Authentication.GetS3StorageUploadUrl.Run(Request(), log)), 400, "Please provide a file and an upload path");

        [Fact]
        public async Task GetStorageToken() =>
            AssertRejected(await Authentication.GetStorageToken.Run(Request(method: "POST"), log), 403, "refresh_token is missing");

        [Fact]
        public async Task GetUserInfo() =>
            AssertRejected(await Authentication.GetUserInfo.Run(Request(), log), 400, "Email is invalid");

        [Fact]
        public async Task RefreshToken() =>
            AssertRejected(await Authentication.RefreshToken.Run(Request(), log), 400, "refresh_token is missing");

        [Theory]
        [InlineData("", "Email is empty")]
        [InlineData("email=not-an-email", "Email not-an-email is invalid")]
        public async Task SocialSignIn(string query, string error) =>
            AssertRejected(await new SocialSignIn().Run(Request(query, "POST"), log), 400, error);

        [Theory]
        [InlineData("", "Must provide first_name or last_name")]
        [InlineData("first_name=Pat", "email is invalid")]
        public async Task SubscribeNewUser(string query, string error) =>
            AssertRejected(await new SubscribeNewUser().Run(Request(query), log), 400, error);

        [Fact]
        public async Task SubscribeTestUser() =>
            AssertRejected(await new SubscribeTestUser().Run(Request(method: "POST"), log), 401, "client_token is missing");

        [Fact]
        public async Task UpdateRole() =>
            AssertRejected(await Authentication.UpdateRole.Run(Request(), log), 401, "auth_token is missing");

        [Theory]
        [InlineData("", "Email is empty")]
        [InlineData("email=bad%40%40x", "Email bad@@x is invalid")]
        [InlineData("email=nobody%40example.com", "Passcode is empty")]
        public async Task VerifyPasscode(string query, string error) =>
            AssertRejected(await new VerifyPasscode().Run(Request(query), log), 400, error);

        [Theory]
        [InlineData("", "code is missing")]
        [InlineData("code=x", "function_url is missing")]
        public async Task WarmUp(string query, string error) =>
            AssertRejected(await Sync(Authentication.WarmUp.Run(Request(query), log)), 400, error);
    }
}
