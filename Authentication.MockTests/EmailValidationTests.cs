using System.Threading.Tasks;
using Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Authentication.MockTests.TestSetup;

namespace Authentication.MockTests
{
    /// <summary>
    /// IsValidEmailAddress must reject what Cognito rejects with "Username should be an email": an email it lets
    /// through reaches AdminCreateUser and used to fail CheckAccount with 500 (abc.@mail.com, Oct 2026).
    /// </summary>
    public class EmailValidationTests
    {
        public EmailValidationTests() => UseTestConfiguration();

        [Theory]
        [InlineData("x@y.io")]
        [InlineData("a.b@mail.com")]
        [InlineData("a+b@mail.co.uk")]
        [InlineData("a_b-c%d@sub.mail-x.com")]
        [InlineData("abc-@mail.com")]
        [InlineData("1@2go.com")]
        public void ValidEmails(string email) => Assert.True(email.IsValidEmailAddress(), email);

        [Theory]
        [InlineData("abc.@mail.com")]
        [InlineData(".abc@mail.com")]
        [InlineData("a..b@mail.com")]
        [InlineData("abc@-mail.com")]
        [InlineData("abc@mail-.com")]
        [InlineData("abc@sub.-mail.com")]
        [InlineData("abc@mail..com")]
        [InlineData("abc@.mail.com")]
        [InlineData("@mail.com")]
        [InlineData("abc@")]
        [InlineData("bad@@x")]
        public void InvalidEmails(string email) => Assert.False(email.IsValidEmailAddress(), email);

        [Fact]
        public async Task CheckAccountAnswers400ForADotBeforeTheAt()
        {
            var (code, body) = Read(await new CheckAccount().Run(Request("email=nomx.%40mail.com&source=cognito"), NullLogger.Instance));
            Assert.Equal(400, code);
            Assert.Equal("Email nomx.@mail.com is invalid", (string)body["error"]);
        }
    }
}
