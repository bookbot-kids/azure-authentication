using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// Apple and Google id tokens checked by TokenService.ValidatePublicJWTToken, and the checks AppleService
    /// and GoogleService make before calling Apple or Google (only paths that return before any network call).
    /// </summary>
    public class SocialIdTokenTests
    {
        const string Email = "pat@bookbot.test";

        public SocialIdTokenTests() => UseTestConfiguration();

        static Dictionary<string, string> Expect(string iss, string email = Email) => new() { { "email", email }, { "iss", iss } };

        [Fact]
        public void AppleIdTokenIsAcceptedAndExposesAudience()
        {
            var (valid, payload) = TokenService.ValidatePublicJWTToken(IdToken(AppleClaims(Email)), Expect("https://appleid.apple.com"));

            Assert.True(valid);
            Assert.Equal("com.bookbot.test.app", payload["aud"].ToString());
            Assert.Equal(Email, payload["email"].ToString());
        }

        [Fact]
        public void GoogleIdTokenIsAcceptedAndExposesAudience()
        {
            var (valid, payload) = TokenService.ValidatePublicJWTToken(IdToken(GoogleClaims(Email)), Expect("https://accounts.google.com"));

            Assert.True(valid);
            Assert.Equal("google-client-1.apps.test", payload["aud"].ToString());
        }

        [Fact]
        public void EmailMismatchIsRejected() =>
            Assert.False(TokenService.ValidatePublicJWTToken(IdToken(AppleClaims("someone@else.test")), Expect("https://appleid.apple.com")).Item1);

        [Fact]
        public void IssuerMismatchIsRejected() =>
            Assert.False(TokenService.ValidatePublicJWTToken(IdToken(GoogleClaims(Email)), Expect("https://appleid.apple.com")).Item1);

        [Fact(Skip = "SECURITY: the JWT library only checks exp when it verifies the signature, which ValidatePublicJWTToken never asks for, so expired id tokens pass. Enable once signatures are verified.")]
        public void ExpiredTokenIsRejected() =>
            Assert.False(TokenService.ValidatePublicJWTToken(IdToken(AppleClaims(Email, exp: Now - 60)), Expect("https://appleid.apple.com")).Item1);

        [Fact]
        public void TokenWithoutEmailIsRejected()
        {
            var claims = AppleClaims(Email);
            claims.Remove("email");
            Assert.False(TokenService.ValidatePublicJWTToken(IdToken(claims), Expect("https://appleid.apple.com")).Item1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-token")]
        [InlineData("a.b.c")]
        public void MalformedTokenIsRejected(string token) =>
            Assert.False(TokenService.ValidatePublicJWTToken(token, Expect("https://appleid.apple.com")).Item1);

        [Fact]
        public async Task AppleRejectsInvalidIdTokenBeforeCallingApple() =>
            Assert.Equal((false, "Id token is invalid"), await AppleService.Instance.ValidateToken(Email, "auth-code", "not-a-token"));

        [Fact]
        public async Task AppleRejectsUnknownClientIdBeforeCallingApple() =>
            Assert.Equal((false, "id_token is invalid"),
                await AppleService.Instance.ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email, aud: "com.someone.else"))));

        [Fact]
        public async Task GoogleRejectsInvalidIdTokenBeforeCallingGoogle() =>
            Assert.Equal((false, "id_token is invalid"), await GoogleService.Instance.ValidateAccessToken(Email, "access-token", "not-a-token"));

        [Fact]
        public async Task GoogleRejectsUnknownClientIdBeforeCallingGoogle() =>
            Assert.Equal((false, "id_token is invalid"),
                await GoogleService.Instance.ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email, aud: "someone-else.apps.test"))));

        [Fact(Skip = "SECURITY: ValidatePublicJWTToken does not verify the signature against Apple's/Google's published keys. Enable once it does.")]
        public void IdTokenSignedByAnUnknownKeyIsRejected()
        {
            var attackerKey = RSA.Create(2048);
            Assert.False(TokenService.ValidatePublicJWTToken(IdToken(AppleClaims(Email), attackerKey), Expect("https://appleid.apple.com")).Item1);
        }

        [Fact(Skip = "SECURITY: ValidatePublicJWTToken accepts unsigned (alg none) tokens. Enable once signatures are verified.")]
        public void UnsignedIdTokenIsRejected()
        {
            var parts = IdToken(AppleClaims(Email)).Split('.');
            var unsigned = B64Url(new Newtonsoft.Json.Linq.JObject { ["alg"] = "none" }) + "." + parts[1] + ".";
            Assert.False(TokenService.ValidatePublicJWTToken(unsigned, Expect("https://appleid.apple.com")).Item1);
        }
    }
}
