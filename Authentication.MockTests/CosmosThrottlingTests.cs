using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace Authentication.MockTests
{
    /// <summary>
    /// Guards against Cosmos metadata throttling (429, substatus 3200) on resource-token requests:
    /// a throttled permission read must not trigger a create, each Cosmos user is created once per
    /// instance, and the container list is cached and survives a throttled refresh.
    /// </summary>
    public class CosmosThrottlingTests
    {
        static CosmosException Cosmos(HttpStatusCode status, int subStatus = 0) =>
            new CosmosException("test", status, subStatus, "activity", 1);

        // ---------- only a 404 means "missing" ----------

        [Fact]
        public void NotFoundMeansMissing() => Assert.True(CosmosService.IsMissing(Cosmos(HttpStatusCode.NotFound)));

        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests, 3200)]
        [InlineData(HttpStatusCode.TooManyRequests, 0)]
        [InlineData(HttpStatusCode.ServiceUnavailable, 0)]
        [InlineData(HttpStatusCode.RequestTimeout, 0)]
        [InlineData(HttpStatusCode.Forbidden, 0)]
        [InlineData(HttpStatusCode.Conflict, 0)]
        public void OtherFailuresDoNotMeanMissing(HttpStatusCode status, int subStatus) =>
            Assert.False(CosmosService.IsMissing(Cosmos(status, subStatus)));

        // ---------- each user is created once per instance ----------

        [Fact]
        public async Task ParallelCallersShareOneAttempt()
        {
            var calls = 0;
            var release = new TaskCompletionSource<bool>();
            var once = new OncePerKey("test-parallel", async _ => { Interlocked.Increment(ref calls); return await release.Task; }, TimeSpan.FromMinutes(30));

            var callers = Enumerable.Range(0, 20).Select(_ => once.Run("user-1")).ToList();
            release.SetResult(true);
            await Task.WhenAll(callers);

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task SuccessIsRemembered()
        {
            var calls = 0;
            var once = new OncePerKey("test-remember", _ => { calls++; return Task.FromResult(true); }, TimeSpan.FromMinutes(30));

            await once.Run("user-1");
            await once.Run("user-1");
            await once.Run("user-1");

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task FailureIsRetriedOnTheNextCall()
        {
            var calls = 0;
            var once = new OncePerKey("test-retry", _ => { calls++; return Task.FromResult(calls > 1); }, TimeSpan.FromMinutes(30));

            await once.Run("user-1"); // fails, e.g. throttled
            await once.Run("user-1"); // retried, succeeds
            await once.Run("user-1"); // remembered

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task KeysAreIndependent()
        {
            var seen = new List<string>();
            var once = new OncePerKey("test-keys", key => { lock (seen) { seen.Add(key); } return Task.FromResult(true); }, TimeSpan.FromMinutes(30));

            await Task.WhenAll(once.Run("admin"), once.Run("user-1"), once.Run("user-2"), once.Run("admin"));

            Assert.Equal(new[] { "admin", "user-1", "user-2" }, seen.OrderBy(k => k));
        }

        [Fact]
        public async Task AnExceptionReachesCallersAndIsNotRemembered()
        {
            var calls = 0;
            var once = new OncePerKey("test-throw", _ => { if (++calls == 1) throw new InvalidOperationException("boom"); return Task.FromResult(true); }, TimeSpan.FromMinutes(30));

            await Assert.ThrowsAsync<InvalidOperationException>(() => once.Run("user-1"));
            await once.Run("user-1");

            Assert.Equal(2, calls);
        }

        // ---------- container list cache ----------

        sealed class Clock
        {
            public DateTime Now = new DateTime(2026, 10, 10, 15, 0, 0, DateTimeKind.Utc);
        }

        [Fact]
        public async Task ServesTheCachedListUntilItExpires()
        {
            var clock = new Clock();
            var loads = 0;
            var tables = new CachedValue<List<string>>(() => { loads++; return Task.FromResult(new List<string> { "User", $"v{loads}" }); }, TimeSpan.FromMinutes(20), () => clock.Now);

            Assert.Equal("v1", (await tables.Get())[1]);
            clock.Now = clock.Now.AddMinutes(19);
            Assert.Equal("v1", (await tables.Get())[1]);
            clock.Now = clock.Now.AddMinutes(2);
            Assert.Equal("v2", (await tables.Get())[1]);
            Assert.Equal(2, loads);
        }

        [Fact]
        public async Task ThrottledRefreshServesTheLastListAndBacksOff()
        {
            var clock = new Clock();
            var loads = 0;
            var throttled = false;
            var tables = new CachedValue<List<string>>(() =>
            {
                loads++;
                if (throttled) throw Cosmos(HttpStatusCode.TooManyRequests, 3200);
                return Task.FromResult(new List<string> { "User", "Profile" });
            }, TimeSpan.FromMinutes(20), () => clock.Now);

            await tables.Get();
            throttled = true;
            clock.Now = clock.Now.AddMinutes(21);

            Assert.Equal(new[] { "User", "Profile" }, await tables.Get()); // refresh throttled: last list, no 500
            Assert.Equal(new[] { "User", "Profile" }, await tables.Get()); // no new attempt right away
            Assert.Equal(2, loads);

            clock.Now = clock.Now.AddMinutes(1);
            await tables.Get();                                             // retried after a minute
            Assert.Equal(3, loads);
        }

        [Fact]
        public async Task FirstLoadFailureIsReported()
        {
            var tables = new CachedValue<List<string>>(() => throw Cosmos(HttpStatusCode.TooManyRequests, 3200), TimeSpan.FromMinutes(20));

            var error = await Assert.ThrowsAsync<CosmosException>(() => tables.Get());
            Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        }

        [Fact]
        public async Task ParallelRequestsShareOneLoad()
        {
            var loads = 0;
            var release = new TaskCompletionSource<List<string>>();
            var tables = new CachedValue<List<string>>(() => { Interlocked.Increment(ref loads); return release.Task; }, TimeSpan.FromMinutes(20));

            var callers = Enumerable.Range(0, 10).Select(_ => tables.Get()).ToList();
            release.SetResult(new List<string> { "User" });
            await Task.WhenAll(callers);

            Assert.Equal(1, loads);
            Assert.All(callers, c => Assert.Equal(new[] { "User" }, c.Result));
        }
    }
}
