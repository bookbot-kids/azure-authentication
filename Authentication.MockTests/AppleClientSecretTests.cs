using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Authentication.Shared;
using Authentication.Shared.Services;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// The client secret AppleService signs with the .p8 key (ES256, PKCS#8) before redeeming an auth code.
    /// Apple requires: alg ES256 + kid header, iss = team id, sub = client id, aud = https://appleid.apple.com,
    /// and a raw 64-byte R||S signature (not DER).
    /// </summary>
    public class AppleClientSecretTests
    {
        const string ClientId = "com.bookbot.test.web";

        public AppleClientSecretTests() => UseTestConfiguration();

        // Same arguments AppleService.GenerateSecretToken passes
        static string Generate(string secret = null, DateTime? expires = null) =>
            TokenService.GenerateAppleToken(secret ?? Configurations.Apple.AppleSecret, Configurations.Apple.AppleClientId, ClientId,
                Configurations.Apple.AppleTeamId, "https://appleid.apple.com", expires ?? DateTime.UtcNow.AddDays(1));

        static ECDsa PublicKey()
        {
            var publicKey = ECDsa.Create();
            publicKey.ImportParameters(AppleKey.ExportParameters(false));
            return publicKey;
        }

        [Fact]
        public void HasTheHeaderAndClaimsAppleExpects()
        {
            var expires = DateTime.UtcNow.AddDays(1);
            var token = Generate(expires: expires);
            var header = Part(token, 0);
            var payload = Part(token, 1);

            Assert.Equal("ES256", (string)header["alg"]);
            Assert.Equal("TESTKEY123", (string)header["kid"]);
            Assert.Equal("TESTTEAM01", (string)payload["iss"]);
            Assert.Equal(ClientId, (string)payload["sub"]);
            Assert.Equal("https://appleid.apple.com", (string)payload["aud"]);
            Assert.InRange((long)payload["exp"], new DateTimeOffset(expires).ToUnixTimeSeconds() - 1, new DateTimeOffset(expires).ToUnixTimeSeconds());
        }

        [Fact]
        public void SignatureIsRawP1363AndVerifiesWithThePublicKey()
        {
            var parts = Generate().Split('.');
            var signature = FromB64Url(parts[2]);

            Assert.Equal(64, signature.Length);
            Assert.True(PublicKey().VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature,
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Fact]
        public void ValidatesAsAJwtWithThePublicKey()
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(Generate(), new TokenValidationParameters
            {
                IssuerSigningKey = new ECDsaSecurityKey(PublicKey()),
                ValidIssuer = "TESTTEAM01",
                ValidAudience = "https://appleid.apple.com",
                ValidateLifetime = true,
            }, out _);

            Assert.Contains(principal.Claims, c => c.Value == ClientId);
        }

        [Fact]
        public void EachSecretHasAFreshSignature()
        {
            // ECDSA signatures are randomized; both must still verify
            var first = Generate().Split('.');
            var second = Generate().Split('.');
            Assert.NotEqual(first[2], second[2]);
            Assert.True(PublicKey().VerifyData(Encoding.ASCII.GetBytes(second[0] + "." + second[1]), FromB64Url(second[2]),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Fact]
        public void AcceptsTheKeyWithLineBreaksLikeAP8File()
        {
            var wrapped = string.Join("\n", Chunk(Configurations.Apple.AppleSecret, 64));
            var parts = Generate(wrapped).Split('.');
            Assert.True(PublicKey().VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), FromB64Url(parts[2]),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Fact]
        public void RejectsAKeyThatStillHasPemHeaders()
        {
            var pem = "-----BEGIN PRIVATE KEY-----\n" + Configurations.Apple.AppleSecret + "\n-----END PRIVATE KEY-----";
            Assert.Throws<FormatException>(() => Generate(pem));
        }

        [Fact]
        public void RejectsAKeyThatIsNotPkcs8() =>
            Assert.ThrowsAny<CryptographicException>(() => Generate(Convert.ToBase64String(Encoding.UTF8.GetBytes("not a key"))));

        [Fact]
        public void RejectsAnRsaKey() =>
            Assert.ThrowsAny<CryptographicException>(() => Generate(Convert.ToBase64String(RSA.Create(2048).ExportPkcs8PrivateKey())));

        static string[] Chunk(string text, int size) =>
            Enumerable.Range(0, (text.Length + size - 1) / size).Select(i => text.Substring(i * size, Math.Min(size, text.Length - i * size))).ToArray();
    }
}
