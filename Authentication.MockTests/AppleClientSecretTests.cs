using System;
using System.Formats.Asn1;
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

        // Throwaway key made by OpenSSL (openssl ecparam -genkey | openssl pkcs8 -topk8 -nocrypt), an independent PKCS#8 writer
        const string OpenSslPkcs8 = "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgSNQkrNWIodJfTzSMJIjxv7c2BNy871/q3hICJhW9GNShRANCAATFib1VYweExoqpRnYSHC6UBvETRoxeg/vHnDRmEy+8q/KyaBKaqx6ygxdIEF/ojUs5/XEWj1f61/uN44fbdQrU";
        const string OpenSslPublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAExYm9VWMHhMaKqUZ2EhwulAbxE0aMXoP7x5w0ZhMvvKvysmgSmqsesoMXSBBf6I1LOf1xFo9X+tf7jeOH23UK1A==";

        [Fact]
        public void SignsWithAnOpenSslKeyAndVerifiesWithOpenSslsPublicKey()
        {
            var publicKey = ECDsa.Create();
            publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(OpenSslPublicKey), out _);
            var parts = Generate(OpenSslPkcs8).Split('.');

            Assert.True(publicKey.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), FromB64Url(parts[2]),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        [Theory]
        [InlineData(true, true)]   // Apple .p8 layout: curve repeated inside ECPrivateKey [0], public key [1]
        [InlineData(false, true)]  // OpenSSL / .NET layout
        [InlineData(true, false)]  // no public key: it is derived from the private key
        public void AcceptsEveryEcPrivateKeyLayout(bool curveInside, bool publicKeyInside)
        {
            var parts = Generate(Convert.ToBase64String(Pkcs8(AppleKey.ExportParameters(true), curveInside, publicKeyInside))).Split('.');
            Assert.True(PublicKey().VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), FromB64Url(parts[2]),
                HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }

        // RFC 5208 PrivateKeyInfo wrapping an RFC 5915 ECPrivateKey
        static byte[] Pkcs8(ECParameters key, bool curveInside, bool publicKeyInside)
        {
            var ecPrivateKey = new AsnWriter(AsnEncodingRules.DER);
            using (ecPrivateKey.PushSequence())
            {
                ecPrivateKey.WriteInteger(1);
                ecPrivateKey.WriteOctetString(key.D);
                if (curveInside)
                {
                    using (ecPrivateKey.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                    {
                        ecPrivateKey.WriteObjectIdentifier("1.2.840.10045.3.1.7");
                    }
                }

                if (publicKeyInside)
                {
                    using (ecPrivateKey.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true)))
                    {
                        ecPrivateKey.WriteBitString(new byte[] { 0x04 }.Concat(key.Q.X).Concat(key.Q.Y).ToArray());
                    }
                }
            }

            var privateKeyInfo = new AsnWriter(AsnEncodingRules.DER);
            using (privateKeyInfo.PushSequence())
            {
                privateKeyInfo.WriteInteger(0);
                using (privateKeyInfo.PushSequence())
                {
                    privateKeyInfo.WriteObjectIdentifier("1.2.840.10045.2.1");
                    privateKeyInfo.WriteObjectIdentifier("1.2.840.10045.3.1.7");
                }

                privateKeyInfo.WriteOctetString(ecPrivateKey.Encode());
            }

            return privateKeyInfo.Encode();
        }

        static string[] Chunk(string text, int size) =>
            Enumerable.Range(0, (text.Length + size - 1) / size).Select(i => text.Substring(i * size, Math.Min(size, text.Length - i * size))).ToArray();
    }
}
