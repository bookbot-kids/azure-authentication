using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Authentication.MockTests.TestSetup;

namespace Authentication.MockTests
{
    /// <summary>
    /// Auto sign-in tokens: AES-256-CBC/PKCS7 of "userId;expiryMs", key = SignInKey, IV = first 16 chars of the user id.
    /// Vectors were produced with OpenSSL 3.6 (an independent implementation), e.g.
    /// printf 'a1b2c3d4e5f6a7b8-9c0d;4102444800000' | openssl enc -aes-256-cbc -K hex(SignInKey) -iv hex(a1b2c3d4e5f6a7b8) -base64 -A
    /// </summary>
    public class SignInTokenTests
    {
        const string UserId = "a1b2c3d4e5f6a7b8-9c0d";
        const string Iv = "a1b2c3d4e5f6a7b8";
        const string ValidUntil2100 = "fb2IJy+yWTdpKjuBAEdBdYlIyTM4PZMTx1XGqPBrEbb40g/e7WA8Vec57c3Wy9q6"; // "a1b2c3d4e5f6a7b8-9c0d;4102444800000"
        const string ExpiredIn2000 = "fb2IJy+yWTdpKjuBAEdBdaLuji9sIIJCwQWMsbs2NZ6eFr7jAkQwIL6MiPeYFRJv";  // "a1b2c3d4e5f6a7b8-9c0d;946684800000"
        const string OtherUser = "lCL3jtk8d06RFk7IdAW2ejZJKiQS081y+dgZy1veTb6wx+RrN342hi4U0gHN0AD0";     // "zzzzzzzzzzzzzzzz-0000;4102444800000"
        const string NoSeparator = "+Mfp9vhnwrJM3LHcobeDTA==";                                             // "no-separator"
        const string WrongKey = "Bucpf3v5Xy7+bCZ8VbBewQRE1tI16lKaoWtDPM0qFq6koBC+Njsizr93FregeIQj";      // valid text, key "ffff…"

        public SignInTokenTests() => UseTestConfiguration();

        [Fact]
        public void DecryptsATokenProducedByOpenSsl() =>
            Assert.Equal("a1b2c3d4e5f6a7b8-9c0d;4102444800000", TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, Iv, ValidUntil2100));

        [Fact]
        public void RoundTripsWithDotNetAes()
        {
            const string text = "a1b2c3d4e5f6a7b8-9c0d;1893456000000";
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(Configurations.JWTToken.SignInKey);
            aes.IV = Encoding.UTF8.GetBytes(Iv);
            var cipher = aes.EncryptCbc(Encoding.UTF8.GetBytes(text), aes.IV, PaddingMode.PKCS7);

            Assert.Equal(text, TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, Iv, Convert.ToBase64String(cipher)));
        }

        [Fact]
        public void WrongKeyDoesNotDecrypt() =>
            Assert.NotEqual("a1b2c3d4e5f6a7b8-9c0d;4102444800000", TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, Iv, WrongKey));

        [Fact]
        public void WrongIvDoesNotRecoverTheUserId() =>
            Assert.DoesNotContain(UserId, TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, "zzzzzzzzzzzzzzzz", ValidUntil2100) ?? "");

        [Theory]
        [InlineData("not base64!")]
        [InlineData("AAAA")]
        public void GarbageReturnsNull(string token) => Assert.Null(TokenService.EASDecrypt(Configurations.JWTToken.SignInKey, Iv, token));

        [Theory]
        [InlineData(ExpiredIn2000, "sign_in_token is expired")]
        [InlineData(OtherUser, "sign_in_token is not valid")]
        [InlineData(NoSeparator, "sign_in_token is not valid")]
        [InlineData(WrongKey, "sign_in_token is not valid")]
        public async Task CheckAccountRejects(string token, string error)
        {
            var result = await new CheckAccount().Run(Request($"user_id={UserId}&sign_in_token={WebUtility.UrlEncode(token)}"), NullLogger.Instance);
            var (status, body) = Read(result);

            Assert.Equal(400, status);
            Assert.Equal(error, (string)body["error"]);
        }
    }
}
