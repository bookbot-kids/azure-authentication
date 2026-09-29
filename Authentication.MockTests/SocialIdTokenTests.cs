using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Services.Responses;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Refit;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// Sign in with Apple / Google. Id tokens must be signed by the provider (checked against its published keys, here a
    /// local test key), come from the right issuer, be unexpired and match the email; Apple must redeem the auth code;
    /// Google access tokens must belong to the email and to our Google project. Apple and Google APIs are faked.
    /// </summary>
    public class SocialIdTokenTests
    {
        const string Email = "pat@bookbot.test";
        const string GoogleClient = "123456789012-abc123def.apps.googleusercontent.com";

        public SocialIdTokenTests() => UseTestConfiguration();

        static Task<(bool, IDictionary<string, object>)> Validate(string token, TokenService.OpenIdProvider provider, string email = Email) =>
            TokenService.ValidateIdToken(token, email, provider);

        // ---------- id token validation ----------

        [Fact]
        public async Task AppleIdTokenIsAcceptedAndExposesAudience()
        {
            var (valid, payload) = await Validate(IdToken(AppleClaims(Email)), TokenService.OpenIdProvider.Apple);

            Assert.True(valid);
            Assert.Equal("com.bookbot.test.app", payload["aud"].ToString());
        }

        [Theory]
        [InlineData("https://accounts.google.com")]
        [InlineData("accounts.google.com")]
        public async Task GoogleIdTokenIsAcceptedWithEitherIssuerForm(string issuer)
        {
            var (valid, payload) = await Validate(IdToken(GoogleClaims(Email, iss: issuer)), TokenService.OpenIdProvider.Google);

            Assert.True(valid);
            Assert.Equal(GoogleClient, payload["aud"].ToString());
        }

        [Fact]
        public async Task IdTokenSignedByAnUnknownKeyIsRejected() =>
            Assert.False((await Validate(IdToken(AppleClaims(Email), RSA.Create(2048)), TokenService.OpenIdProvider.Apple)).Item1);

        [Fact]
        public async Task UnsignedIdTokenIsRejected()
        {
            var parts = IdToken(AppleClaims(Email)).Split('.');
            var unsigned = B64Url(new JObject { ["alg"] = "none" }) + "." + parts[1] + ".";
            Assert.False((await Validate(unsigned, TokenService.OpenIdProvider.Apple)).Item1);
        }

        [Fact]
        public async Task TamperedIdTokenIsRejected()
        {
            var parts = IdToken(AppleClaims(Email)).Split('.');
            var claims = AppleClaims("victim@bookbot.test");
            Assert.False((await Validate($"{parts[0]}.{B64Url(claims)}.{parts[2]}", TokenService.OpenIdProvider.Apple, "victim@bookbot.test")).Item1);
        }

        [Fact]
        public async Task ExpiredTokenIsRejected() =>
            Assert.False((await Validate(IdToken(AppleClaims(Email, exp: Now - 600)), TokenService.OpenIdProvider.Apple)).Item1);

        [Fact]
        public async Task EmailMismatchIsRejected() =>
            Assert.False((await Validate(IdToken(AppleClaims("someone@else.test")), TokenService.OpenIdProvider.Apple)).Item1);

        [Fact]
        public async Task TokenFromTheOtherProviderIsRejected() =>
            Assert.False((await Validate(IdToken(GoogleClaims(Email)), TokenService.OpenIdProvider.Apple)).Item1);

        [Fact]
        public async Task TokenWithoutEmailIsRejected()
        {
            var claims = AppleClaims(Email);
            claims.Remove("email");
            Assert.False((await Validate(IdToken(claims), TokenService.OpenIdProvider.Apple)).Item1);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-token")]
        [InlineData("a.b.c")]
        public async Task MalformedTokenIsRejected(string token) =>
            Assert.False((await Validate(token, TokenService.OpenIdProvider.Apple)).Item1);

        [Fact]
        public async Task RotatedKeysAreRefreshedOnce()
        {
            var requests = new List<bool>();
            await WithKeys(refresh =>
            {
                requests.Add(refresh);
                return Task.FromResult<ICollection<SecurityKey>>(refresh ? ProviderKeys() : new List<SecurityKey>());
            }, async () => Assert.True((await Validate(IdToken(AppleClaims(Email)), TokenService.OpenIdProvider.Apple)).Item1));

            Assert.Equal(new[] { false, true }, requests);
        }

        [Fact]
        public async Task UnknownKeyAfterRefreshIsRejected()
        {
            var requests = 0;
            await WithKeys(_ =>
            {
                requests++;
                return Task.FromResult<ICollection<SecurityKey>>(new List<SecurityKey>());
            }, async () => Assert.False((await Validate(IdToken(AppleClaims(Email)), TokenService.OpenIdProvider.Apple)).Item1));

            Assert.Equal(2, requests);
        }

        [Fact]
        public async Task UnavailableProviderKeysRejectTheToken() =>
            await WithKeys(_ => throw new HttpRequestException("appleid.apple.com unreachable"),
                async () => Assert.False((await Validate(IdToken(AppleClaims(Email)), TokenService.OpenIdProvider.Apple)).Item1));

        // Only this test class touches the providers' key source, and xUnit runs a class's tests one at a time
        static async Task WithKeys(Func<bool, Task<ICollection<SecurityKey>>> keys, Func<Task> test)
        {
            var original = TokenService.OpenIdProvider.Apple.SigningKeys;
            TokenService.OpenIdProvider.Apple.SigningKeys = keys;
            try
            {
                await test();
            }
            finally
            {
                TokenService.OpenIdProvider.Apple.SigningKeys = original;
            }
        }

        // ---------- Sign in with Apple ----------

        [Fact]
        public async Task AppleSignInSucceedsWhenAppleRedeemsTheCode()
        {
            var apple = new FakeApple { Response = new AppleTokenResponse { AccessToken = "apple-access-token" } };

            Assert.Equal((true, ""), await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email))));

            var request = Assert.Single(apple.Requests);
            Assert.Equal("com.bookbot.test.app", request["client_id"]);
            Assert.Equal("auth-code", request["code"]);
            Assert.Equal("authorization_code", request["grant_type"]);
            var secret = request["client_secret"].ToString().Split('.');
            Assert.Equal("ES256", (string)Part(string.Join(".", secret), 0)["alg"]);
            Assert.True(AppleKey.VerifyData(Encoding.ASCII.GetBytes(secret[0] + "." + secret[1]), FromB64Url(secret[2]),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Fact]
        public async Task ForgedAppleIdTokenIsRejectedBeforeCallingApple()
        {
            var apple = new FakeApple { Response = new AppleTokenResponse { AccessToken = "apple-access-token" } };

            Assert.Equal((false, "Id token is invalid"),
                await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email), RSA.Create(2048))));
            Assert.Empty(apple.Requests);
        }

        [Fact]
        public async Task AppleRejectsUnknownClientIdBeforeCallingApple()
        {
            var apple = new FakeApple();

            Assert.Equal((false, "id_token is invalid"),
                await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email, aud: "com.someone.else"))));
            Assert.Empty(apple.Requests);
        }

        [Fact]
        public async Task AppleSignInFailsWhenAppleRejectsTheCode()
        {
            var apple = new FakeApple { Error = await ApiError(HttpStatusCode.BadRequest, "{\"error\":\"invalid_grant\"}") };

            Assert.Equal((false, "Auth code is invalid"), await new AppleService(apple).ValidateToken(Email, "used-code", IdToken(AppleClaims(Email))));
        }

        [Fact]
        public async Task AppleSignInFailsWithoutAnAccessToken()
        {
            var apple = new FakeApple { Response = new AppleTokenResponse() };

            Assert.Equal((false, "Auth code is invalid"), await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email))));
        }

        [Fact]
        public async Task AppleSignInFailsWhenAppleIsUnreachable()
        {
            var apple = new FakeApple { Error = new HttpRequestException("appleid.apple.com unreachable") };

            Assert.Equal((false, "Auth code is invalid"), await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email))));
        }

        [Fact]
        public async Task AppSignInRedeemsWithTheConfiguredRedirectUri()
        {
            var apple = new FakeApple { Response = new AppleTokenResponse { AccessToken = "apple-access-token" } };

            await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email)));

            Assert.Equal(Authentication.Shared.Configurations.Apple.AppleRedirectUrl, Assert.Single(apple.Requests)["redirect_uri"]);
        }

        [Fact]
        public async Task WebSignInRedeemsWithTheRedirectUriItSignedInWith()
        {
            // Apple only redeems a code with the URI it was issued to: the website's, not the Cognito one
            var apple = new FakeApple { Response = new AppleTokenResponse { AccessToken = "apple-access-token" } };

            Assert.Equal((true, ""), await new AppleService(apple).ValidateToken(Email, "auth-code",
                IdToken(AppleClaims(Email, aud: "com.bookbot.test.web")), "https://teacher.bookbot.test/auth/apple/callback"));
            Assert.Equal("https://teacher.bookbot.test/auth/apple/callback", Assert.Single(apple.Requests)["redirect_uri"]);
        }

        [Theory]
        [InlineData("http://teacher.bookbot.test/auth/apple/callback")]
        [InlineData("/auth/apple/callback")]
        [InlineData("javascript:alert(1)")]
        [InlineData("not a url")]
        public async Task InvalidRedirectUriIsRejectedBeforeCallingApple(string redirectUri)
        {
            var apple = new FakeApple { Response = new AppleTokenResponse { AccessToken = "apple-access-token" } };

            Assert.Equal((false, "redirect_uri is invalid"), await new AppleService(apple).ValidateToken(Email, "auth-code", IdToken(AppleClaims(Email)), redirectUri));
            Assert.Empty(apple.Requests);
        }

        [Fact]
        public async Task AppleRejectsInvalidIdToken() =>
            Assert.Equal((false, "Id token is invalid"), await new AppleService(new FakeApple()).ValidateToken(Email, "auth-code", "not-a-token"));

        // ---------- Google sign in ----------

        static GoogleTokenResponse AccessToken(string email = Email, string aud = GoogleClient, long? exp = null) =>
            new() { Aud = aud, Azp = aud, Email = email, Exp = (exp ?? Now + 600).ToString() };

        [Fact]
        public async Task GoogleSignInSucceedsWithValidIdAndAccessTokens()
        {
            var google = new FakeGoogle { Response = AccessToken() };

            Assert.Equal((true, ""), await new GoogleService(google, google).ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));
            Assert.Equal("access-token", Assert.Single(google.Requests));
        }

        [Fact]
        public async Task GoogleAccessTokenForAnotherClientOfOurProjectIsAccepted() =>
            // e.g. the iOS/Android OAuth client, not listed in GoogleClientIds
            Assert.Equal((true, ""), await new GoogleService(new FakeGoogle { Response = AccessToken(aud: "123456789012-zzz999yyy.apps.googleusercontent.com") }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));

        [Fact]
        public async Task GoogleAccessTokenFromAnotherProjectIsRejected() =>
            Assert.Equal((false, "access_token is invalid"), await new GoogleService(new FakeGoogle { Response = AccessToken(aud: "999999999999-other.apps.googleusercontent.com") }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));

        [Fact]
        public async Task GoogleAccessTokenForAnotherEmailIsRejected() =>
            Assert.Equal((false, "access_token is invalid"), await new GoogleService(new FakeGoogle { Response = AccessToken(email: "someone@else.test") }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));

        [Fact]
        public async Task ExpiredGoogleAccessTokenIsRejected() =>
            Assert.Equal((false, "access_token is invalid"), await new GoogleService(new FakeGoogle { Response = AccessToken(exp: Now - 60) }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));

        [Fact]
        public async Task GoogleRejectedAccessTokenIsRejected() =>
            Assert.Equal((false, "access_token is invalid"), await new GoogleService(new FakeGoogle { Error = await ApiError(HttpStatusCode.BadRequest, "{\"error\":\"invalid_token\"}") }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email))));

        [Fact]
        public async Task GoogleSignInWithoutIdTokenReliesOnTheAccessToken() =>
            Assert.Equal((true, ""), await new GoogleService(new FakeGoogle { Response = AccessToken() }, null).ValidateAccessToken(Email, "access-token", null));

        [Fact]
        public async Task ForgedGoogleIdTokenIsRejectedBeforeCallingGoogle()
        {
            var google = new FakeGoogle { Response = AccessToken() };

            Assert.Equal((false, "id_token is invalid"),
                await new GoogleService(google, google).ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email), RSA.Create(2048))));
            Assert.Empty(google.Requests);
        }

        [Fact]
        public async Task GoogleRejectsUnknownIdTokenClient() =>
            Assert.Equal((false, "id_token is invalid"), await new GoogleService(new FakeGoogle { Response = AccessToken() }, null)
                .ValidateAccessToken(Email, "access-token", IdToken(GoogleClaims(Email, aud: "123456789012-notlisted.apps.googleusercontent.com"))));

        [Theory]
        [InlineData("123456789012-abc123def.apps.googleusercontent.com", true)]
        [InlineData("123456789012-anyother1.apps.googleusercontent.com", true)]
        [InlineData("com.googleusercontent.apps.123456789012-mno789pqr", true)]
        [InlineData("com.googleusercontent.apps.123456789012-other2", true)]
        [InlineData("999999999999-abc123def.apps.googleusercontent.com", false)]
        [InlineData("1234567890123-abc.apps.googleusercontent.com", false)]
        [InlineData("123456789012-abc.apps.googleusercontent.com.evil.test", false)]
        [InlineData("not-a-client-id", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void OnlyClientsOfOurGoogleProjectsAreOurs(string clientId, bool ours) =>
            Assert.Equal(ours, GoogleService.IsOurGoogleClient(clientId));

        // ---------- fakes ----------

        static Task<ApiException> ApiError(HttpStatusCode status, string body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://provider.test/token");
            var response = new HttpResponseMessage(status) { Content = new StringContent(body), RequestMessage = request };
            return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
        }

        class FakeApple : AppleService.IAppleRestApi
        {
            public readonly List<Dictionary<string, object>> Requests = new();
            public AppleTokenResponse Response;
            public Exception Error;

            public Task<AppleTokenResponse> ValidateIdToken(Dictionary<string, object> data)
            {
                Requests.Add(data);
                return Error != null ? Task.FromException<AppleTokenResponse>(Error) : Task.FromResult(Response);
            }
        }

        class FakeGoogle : GoogleService.IGoogleRestApi, GoogleService.IFirebaseRestApi
        {
            public readonly List<string> Requests = new();
            public GoogleTokenResponse Response;
            public Exception Error;

            public Task<GoogleTokenResponse> ValidateAccessToken(string accessToken)
            {
                Requests.Add(accessToken);
                return Error != null ? Task.FromException<GoogleTokenResponse>(Error) : Task.FromResult(Response);
            }

            public Task<DeepLink> GenerateShortLink(string key, ExpandoObject body) => throw new NotSupportedException();
        }
    }
}
