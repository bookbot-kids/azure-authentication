using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Refit;
using Xunit;
using static Authentication.MockTests.TestSetup;
using static Authentication.MockTests.TestTokens;

namespace Authentication.MockTests
{
    /// <summary>
    /// Only Cognito rejecting a refresh token may become a 401: the apps sign the user out and wipe local data on 401.
    /// A token request Cognito never answered (dropped connection, timeout, 5xx) is retried once, then becomes 503.
    /// In production every GetResourceTokens 401 of the previous 90 days was a dropped connection, not a rejection.
    /// </summary>
    public class CognitoTokenFailureTests
    {
        public CognitoTokenFailureTests() => UseTestConfiguration();

        static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}";

        // ---------- the retry rule ----------

        static Task<ApiException> CognitoError(HttpStatusCode status, string body = "{\"error\":\"invalid_grant\"}")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://cognito.test/oauth2/token");
            return ApiException.Create(request, HttpMethod.Post, new HttpResponseMessage(status) { Content = new StringContent(body), RequestMessage = request }, new RefitSettings());
        }

        static async Task<(ADToken token, Exception error, int calls, List<TimeSpan> waits)> Run(params Func<Task<ADToken>>[] answers)
        {
            var calls = 0;
            var waits = new List<TimeSpan>();
            try
            {
                var token = await AWSService.RequestAccessToken(() => answers[Math.Min(calls++, answers.Length - 1)](),
                    wait => { waits.Add(wait); return Task.CompletedTask; });
                return (token, null, calls, waits);
            }
            catch (Exception ex)
            {
                return (null, ex, calls, waits);
            }
        }

        static Func<Task<ADToken>> Ok => () => Task.FromResult(new ADToken { AccessToken = "access" });
        static Func<Task<ADToken>> Throw(Exception ex) => () => Task.FromException<ADToken>(ex);

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task CognitoRejectingTheTokenIsNotRetried(HttpStatusCode status)
        {
            var (token, error, calls, waits) = await Run(Throw(await CognitoError(status)));

            Assert.Null(token);
            Assert.Null(error);
            Assert.Equal(1, calls);
            Assert.Empty(waits);
        }

        public static IEnumerable<object[]> Transient() => new[]
        {
            new object[] { "dropped connection", new HttpRequestException("An error occurred while sending the request.") },
            new object[] { "timeout", new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout") },
            new object[] { "socket reset", new IOException("Unable to read data from the transport connection") },
            new object[] { "unreadable answer", new JsonReaderException("Unexpected character") },
        };

        [Theory]
        [MemberData(nameof(Transient))]
        public async Task NoAnswerIsRetriedOnce(string what, Exception failure)
        {
            var (token, error, calls, waits) = await Run(Throw(failure), Ok);

            Assert.True(token?.AccessToken == "access", what);
            Assert.Null(error);
            Assert.Equal(2, calls);
            Assert.Equal(new[] { TimeSpan.FromMilliseconds(300) }, waits);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.RequestTimeout)]
        public async Task CognitoErrorsAreRetriedOnce(HttpStatusCode status)
        {
            var (token, _, calls, _) = await Run(Throw(await CognitoError(status, "{}")), Ok);

            Assert.Equal("access", token?.AccessToken);
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task TwoFailuresMeanCognitoIsUnavailable()
        {
            var (token, error, calls, _) = await Run(Throw(new HttpRequestException("dropped")));

            Assert.Null(token);
            var unavailable = Assert.IsType<CognitoUnavailableException>(error);
            Assert.IsType<HttpRequestException>(unavailable.InnerException);
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task RejectionAfterAFailureIsStillARejection()
        {
            var (token, error, calls, _) = await Run(Throw(new HttpRequestException("dropped")), Throw(await CognitoError(HttpStatusCode.BadRequest)));

            Assert.Null(token);
            Assert.Null(error);
            Assert.Equal(2, calls);
        }

        // ---------- through the real HTTP stack (FakeCognito) ----------

        [Fact]
        public async Task ValidRefreshTokenGetsAnAccessToken()
        {
            var refresh = Unique("valid-");
            Assert.Equal("access-for-" + refresh, (await AWSService.Instance.GetAccessToken(refresh))?.AccessToken);
            Assert.Equal(1, FakeCognito.Calls(refresh));
        }

        [Fact]
        public async Task RejectedRefreshTokenIsNullWithoutRetry()
        {
            var before = FakeCognito.Calls("rejected");
            Assert.Null(await AWSService.Instance.GetAccessToken("rejected"));
            Assert.Equal(before + 1, FakeCognito.Calls("rejected"));
        }

        [Fact]
        public async Task DroppedConnectionIsRetriedOnANewConnection()
        {
            var refresh = Unique("drop-once-");
            Assert.Equal("access-for-" + refresh, (await AWSService.Instance.GetAccessToken(refresh))?.AccessToken);
            Assert.Equal(2, FakeCognito.Calls(refresh));
        }

        [Fact]
        public async Task ConnectionDroppedTwiceMeansUnavailable()
        {
            var before = FakeCognito.Calls("drop");
            await Assert.ThrowsAsync<CognitoUnavailableException>(() => AWSService.Instance.GetAccessToken("drop"));
            Assert.Equal(before + 2, FakeCognito.Calls("drop"));
        }

        [Fact]
        public async Task CognitoDownMeansUnavailable() =>
            await Assert.ThrowsAsync<CognitoUnavailableException>(() => AWSService.Instance.GetAccessToken("down"));

        [Fact]
        public async Task SuccessWithoutATokenIsNotARejection() =>
            await Assert.ThrowsAsync<CognitoUnavailableException>(() => AWSService.Instance.GetAccessToken("empty"));

        // ---------- what the functions answer ----------

        static void AssertStatus(IActionResult result, int status, string error)
        {
            var (code, body) = Read(result);
            Assert.Equal(status, code);
            Assert.Equal(error, (string)body["error"]);
        }

        const string Unavailable = "sign-in service is temporarily unavailable, please retry";

        [Theory]
        [InlineData("drop")]
        [InlineData("down")]
        public async Task GetResourceTokensAnswers503WhenCognitoGivesNoAnswer(string refresh) =>
            AssertStatus(await GetResourceTokens.Run(Request($"refresh_token={refresh}&source=cognito"), NullLogger.Instance), 503, Unavailable);

        [Fact]
        public async Task GetResourceTokensStillAnswers401ForARejectedToken() =>
            AssertStatus(await GetResourceTokens.Run(Request("refresh_token=rejected&source=cognito"), NullLogger.Instance), 401, "refresh_token is invalid");

        [Fact]
        public async Task GetStorageTokenAnswers503WhenCognitoGivesNoAnswer() =>
            AssertStatus(await GetStorageToken.Run(Request("refresh_token=drop", "POST"), NullLogger.Instance), 503, Unavailable);

        [Fact]
        public async Task GetStorageTokenStillAnswers401ForARejectedToken() =>
            AssertStatus(await GetStorageToken.Run(Request("refresh_token=rejected", "POST"), NullLogger.Instance), 401, "refresh_token is invalid");

        [Fact]
        public async Task ExpireTokenAnswers503WhenCognitoGivesNoAnswer() =>
            AssertStatus(await new ExpireToken().Run(Request("refresh_token=drop&email=pat%40bookbot.test", "POST"), NullLogger.Instance), 503, Unavailable);

        [Fact]
        public async Task DeleteUserAnswers503WhenCognitoGivesNoAnswer() =>
            AssertStatus(await new DeleteUser().Run(Request($"client_token={ClientToken()}&refresh_token=drop&source=cognito", "POST"), NullLogger.Instance), 503, Unavailable);

        [Fact]
        public async Task DeleteUserStillAnswers401ForARejectedToken() =>
            AssertStatus(await new DeleteUser().Run(Request($"client_token={ClientToken()}&refresh_token=rejected&source=cognito", "POST"), NullLogger.Instance), 401, "refresh_token is invalid");
    }
}
