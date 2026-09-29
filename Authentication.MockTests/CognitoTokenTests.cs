using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// Cognito access tokens. TokenService.ValidateCognitoToken downloads the pool's JWKS, so these tests run the
    /// same IdentityModel validation (same TokenValidationParameters) against a JWKS in Cognito's published format,
    /// then read the claims the way AWSService.ValidateAccessToken does. Catches runtime/library changes in RSA
    /// key parsing and JWT validation. Keep the parameters in sync with TokenService.ValidateCognitoToken.
    /// </summary>
    public class CognitoTokenTests
    {
        const string Issuer = "https://cognito-idp.us-west-1.amazonaws.com/us-west-1_TestPool";
        static readonly RSA PoolKey = RSA.Create(2048);

        public CognitoTokenTests() => UseTestConfiguration();

        static ICollection<SecurityKey> SigningKeysFromJwks(RSA key, string kid)
        {
            var p = key.ExportParameters(false);
            var jwks = new JObject
            {
                ["keys"] = new JArray(new JObject
                {
                    ["alg"] = "RS256", ["e"] = B64Url(p.Exponent), ["kid"] = kid, ["kty"] = "RSA", ["n"] = B64Url(p.Modulus), ["use"] = "sig",
                }),
            };
            return new JsonWebKeySet(jwks.ToString()).GetSigningKeys();
        }

        static string AccessToken(RSA key, string kid = "pool-kid", string iss = Issuer, long? exp = null)
        {
            // IdentityModel caches signers per key id; the forged-token tests sign a different key under the pool's kid,
            // so the test signer must not be cached (production only verifies, it never signs with these keys)
            var header = new JwtHeader(new SigningCredentials(new RsaSecurityKey(key) { KeyId = kid }, SecurityAlgorithms.RsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
            });
            var payload = new JwtPayload
            {
                { "sub", "3f1c-user-sub" }, { "cognito:groups", new[] { "subscriber" } }, { "iss", iss },
                { "client_id", "test-client" }, { "origin_jti", "abc" }, { "event_id", "def" }, { "token_use", "access" },
                { "scope", "aws.cognito.signin.user.admin" }, { "auth_time", Now - 60 }, { "exp", exp ?? Now + 3600 },
                { "iat", Now - 60 }, { "jti", "ghi" }, { "username", "3f1c-user-sub" },
            };
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(header, payload));
        }

        // Same parameters as TokenService.ValidateCognitoToken
        static ClaimsPrincipal Validate(string token, ICollection<SecurityKey> keys) =>
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateAudience = false,
                ValidIssuers = new List<string> { Issuer },
                ValidateIssuer = true,
                ValidateIssuerSigningKey = false,
                ValidateLifetime = true,
                IssuerSigningKeys = keys,
            }, out _);

        [Fact]
        public void ValidTokenYieldsUsernameAndGroupLikeAwsService()
        {
            var principal = Validate(AccessToken(PoolKey), SigningKeysFromJwks(PoolKey, "pool-kid"));

            // AWSService.ValidateAccessToken reads the claims this way
            Assert.Equal("3f1c-user-sub", principal.Claims.FirstOrDefault(c => c.Type.Contains("username"))?.Value);
            Assert.Equal("subscriber", principal.Claims.FirstOrDefault(c => c.Type.Contains("cognito:groups"))?.Value);
        }

        [Fact]
        public void ExpiredTokenIsRejected() =>
            Assert.Throws<SecurityTokenExpiredException>(() => Validate(AccessToken(PoolKey, exp: Now - 600), SigningKeysFromJwks(PoolKey, "pool-kid")));

        [Fact]
        public void OtherPoolIsRejected() =>
            Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
                Validate(AccessToken(PoolKey, iss: "https://cognito-idp.us-west-1.amazonaws.com/us-west-1_Other"), SigningKeysFromJwks(PoolKey, "pool-kid")));

        [Fact]
        public void TokenFromUnknownKeyIsRejected() =>
            // ValidateCognitoToken refreshes the JWKS once on this exception, then gives up
            Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() => Validate(AccessToken(RSA.Create(2048), kid: "rotated-kid"), SigningKeysFromJwks(PoolKey, "pool-kid")));

        [Fact]
        public void ForgedSignatureWithKnownKidIsRejected() =>
            Assert.Throws<SecurityTokenInvalidSignatureException>(() => Validate(AccessToken(RSA.Create(2048)), SigningKeysFromJwks(PoolKey, "pool-kid")));

        [Fact]
        public void TamperedPayloadIsRejected()
        {
            var parts = AccessToken(PoolKey).Split('.');
            var claims = Part(string.Join(".", parts), 1);
            claims["cognito:groups"] = new JArray("admin");
            Assert.Throws<SecurityTokenInvalidSignatureException>(() => Validate($"{parts[0]}.{B64Url(claims)}.{parts[2]}", SigningKeysFromJwks(PoolKey, "pool-kid")));
        }

        [Fact]
        public void UnsignedTokenIsRejected()
        {
            var parts = AccessToken(PoolKey).Split('.');
            var unsigned = B64Url(new JObject { ["alg"] = "none", ["typ"] = "JWT" }) + "." + parts[1] + ".";
            Assert.ThrowsAny<SecurityTokenException>(() => Validate(unsigned, SigningKeysFromJwks(PoolKey, "pool-kid")));
        }

        [Fact]
        public async Task MissingTokenIsRejectedWithoutCallingCognito() =>
            Assert.Equal((false, "token is missing", (string)null, (string)null), await AWSService.Instance.ValidateAccessToken(""));
    }
}
