using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// Client tokens (HS256, JWT library) sent by the apps to DeleteUser and SubscribeTestUser.
    /// </summary>
    public class ClientTokenTests
    {
        public ClientTokenTests() => UseTestConfiguration();

        static (bool result, string message, System.Collections.Generic.IDictionary<string, object> payload) Validate(string token) =>
            TokenService.ValidateClientToken(token, Configurations.JWTToken.TokenClientSecret,
                Configurations.JWTToken.TokenIssuer, Configurations.JWTToken.TokenSubject);

        [Fact]
        public void AppStyleTokenIsAccepted()
        {
            var (result, message, payload) = Validate(ClientToken());

            Assert.True(result, message);
            Assert.Equal("success", message);
            Assert.Equal("test-issuer", payload["iss"]);
            Assert.Equal("test-subject", payload["sub"]);
        }

        [Fact]
        public void WrongSecretIsRejected() =>
            Assert.Equal((false, "Token has invalid signature"), Pick(Validate(ClientToken(secret: "other-secret"))));

        [Fact]
        public void KeyMustBeTheBase64OfTheSecret() =>
            Assert.Equal((false, "Token has invalid signature"), Pick(Validate(ClientToken(base64Key: false))));

        [Fact]
        public void TamperedPayloadIsRejected()
        {
            var parts = ClientToken().Split('.');
            var forged = ClientToken(sub: "someone-else").Split('.');
            Assert.Equal((false, "Token has invalid signature"), Pick(Validate($"{parts[0]}.{forged[1]}.{parts[2]}")));
        }

        [Fact]
        public void UnsignedTokenIsRejected() => Assert.False(Validate(ClientToken(alg: "none")).result);

        [Fact]
        public void ExpiredTokenIsRejected() =>
            Assert.Equal((false, "Token has expired"), Pick(Validate(ClientToken(nbf: Now - 1200, exp: Now - 600))));

        [Fact]
        public void NotYetValidTokenIsRejected() => Assert.False(Validate(ClientToken(nbf: Now + 600, exp: Now + 1200)).result);

        [Theory]
        [InlineData("other-issuer", "test-subject")]
        [InlineData("test-issuer", "other-subject")]
        public void WrongIssuerOrSubjectIsRejected(string iss, string sub) =>
            Assert.Equal((false, "Invalid payload"), Pick(Validate(ClientToken(iss: iss, sub: sub))));

        [Theory]
        [InlineData("not-a-token")]
        [InlineData("a.b.c")]
        public void MalformedTokenIsRejected(string token) =>
            Assert.Equal((false, "Can not validate token, unknown error"), Pick(Validate(token)));

        [Fact]
        public async Task SubscribeTestUserAcceptsAppTokenThenChecksEmail() =>
            AssertError(await new SubscribeTestUser().Run(Request("client_token=" + ClientToken(), "POST"), NullLogger.Instance), 400, "Email is empty");

        [Fact]
        public async Task SubscribeTestUserRejectsWrongSecret() =>
            AssertError(await new SubscribeTestUser().Run(Request("client_token=" + ClientToken(secret: "nope"), "POST"), NullLogger.Instance), 401, "Token has invalid signature");

        [Fact]
        public async Task DeleteUserAcceptsAppTokenThenChecksRefreshToken() =>
            AssertError(await new DeleteUser().Run(Request("client_token=" + ClientToken(), "POST"), NullLogger.Instance), 400, "refresh_token is missing");

        [Fact]
        public async Task DeleteUserRejectsExpiredToken() =>
            AssertError(await new DeleteUser().Run(Request("client_token=" + ClientToken(nbf: Now - 1200, exp: Now - 600), "POST"), NullLogger.Instance), 401, "Token has expired");

        static (bool, string) Pick((bool result, string message, System.Collections.Generic.IDictionary<string, object> _) r) => (r.result, r.message);

        static void AssertError(Microsoft.AspNetCore.Mvc.IActionResult result, int status, string error)
        {
            var (code, body) = Read(result);
            Assert.Equal(status, code);
            Assert.Equal(error, (string)body["error"]);
        }
    }
}
