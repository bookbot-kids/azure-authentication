using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Authentication.MockTests
{
    /// <summary>
    /// Builds tokens independently of TokenService, so tests check the server against the clients' formats
    /// rather than against itself.
    /// </summary>
    public static class TestTokens
    {
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static string B64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static string B64Url(JObject json) => B64Url(Encoding.UTF8.GetBytes(json.ToString(Formatting.None)));

        public static byte[] FromB64Url(string text)
        {
            var s = text.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }

        public static JObject Part(string jwt, int index) => JObject.Parse(Encoding.UTF8.GetString(FromB64Url(jwt.Split('.')[index])));

        /// <summary>
        /// Same construction as the apps' WebServiceUtils.generateClientToken (sync_db, jaguar_jwt issueJwtHS256):
        /// HS256 whose HMAC key is the UTF-8 text of base64(utf8(secret)); aud is an array.
        /// </summary>
        public static string ClientToken(string secret = "test-secret", string iss = "test-issuer", string sub = "test-subject",
            long? nbf = null, long? exp = null, string alg = "HS256", bool base64Key = true)
        {
            var now = Now;
            var header = new JObject { ["alg"] = alg, ["typ"] = "JWT" };
            var payload = new JObject
            {
                ["iss"] = iss, ["sub"] = sub, ["aud"] = new JArray("test-audience"),
                ["iat"] = now, ["nbf"] = nbf ?? now, ["exp"] = exp ?? now + 600, ["jti"] = "1234",
            };
            var signingInput = B64Url(header) + "." + B64Url(payload);
            if (alg == "none")
            {
                return signingInput + ".";
            }

            var key = Encoding.UTF8.GetBytes(base64Key ? Convert.ToBase64String(Encoding.UTF8.GetBytes(secret)) : secret);
            return signingInput + "." + B64Url(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(signingInput)));
        }

        public static readonly RSA IdTokenKey = RSA.Create(2048);

        /// <summary>The provider's published signing keys (JWKS) as the validator sees them</summary>
        public static ICollection<SecurityKey> ProviderKeys() =>
            new List<SecurityKey> { new RsaSecurityKey(IdTokenKey.ExportParameters(false)) { KeyId = "test-kid" } };

        /// <summary>RS256 id token like the ones Apple and Google issue</summary>
        public static string IdToken(JObject claims, RSA key = null)
        {
            var header = new JObject { ["alg"] = "RS256", ["kid"] = "test-kid" };
            var signingInput = B64Url(header) + "." + B64Url(claims);
            var signature = (key ?? IdTokenKey).SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return signingInput + "." + B64Url(signature);
        }

        public static JObject AppleClaims(string email, string aud = "com.bookbot.test.app", long? exp = null) => new()
        {
            ["iss"] = "https://appleid.apple.com", ["aud"] = aud, ["exp"] = exp ?? Now + 600, ["iat"] = Now,
            ["sub"] = "001234.abcdef0123456789.0123", ["c_hash"] = "abc", ["email"] = email,
            ["email_verified"] = "true", ["is_private_email"] = "false", ["auth_time"] = Now, ["nonce_supported"] = true,
        };

        public static JObject GoogleClaims(string email, string aud = "123456789012-abc123def.apps.googleusercontent.com", long? exp = null,
            string iss = "https://accounts.google.com") => new()
        {
            ["iss"] = iss, ["azp"] = aud, ["aud"] = aud, ["sub"] = "110169484474386276334",
            ["email"] = email, ["email_verified"] = true, ["at_hash"] = "abc", ["name"] = "Pat Test",
            ["iat"] = Now, ["exp"] = exp ?? Now + 600,
        };
    }
}
