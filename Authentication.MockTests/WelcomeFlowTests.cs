using System;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Xunit;
using static Authentication.MockTests.TestSetup;

namespace Authentication.MockTests
{
    /// <summary>
    /// The new-user welcome steps (Reoon, Branch, QR code, Sendy) are tried twice, and a failing answer counts as a
    /// failure: a Sendy 500 used to pass silently and a Branch 502 surfaced as a NullReferenceException.
    /// </summary>
    public class WelcomeFlowTests
    {
        public WelcomeFlowTests() => UseTestConfiguration();

        static Task Subscribe(string email) =>
            AnalyticsService.Instance.SubscribeToSendyList("test-list", email, name: "Pat", ignoreSpamEmailCheck: true);

        [Fact]
        public async Task SendyAcceptingIsOneCall()
        {
            var email = $"ok-{Guid.NewGuid():N}@example.com";
            await Subscribe(email);
            Assert.Equal(1, FakeSendy.Calls(email));
        }

        [Fact]
        public async Task SendyErrorIsTriedTwiceThenThrows()
        {
            var email = $"down@{Guid.NewGuid():N}.example.com";
            var error = await Assert.ThrowsAsync<Exception>(() => Subscribe(email));

            Assert.Equal("Operation Subscribe sendy failed after 2 attempts", error.Message);
            Assert.Equal(2, FakeSendy.Calls(email));
        }

        [Fact]
        public void FailureLogLinesShareTheAlertText() =>
            Assert.Equal("Welcome flow failed", AnalyticsService.WelcomeFlowFailed);
    }
}
