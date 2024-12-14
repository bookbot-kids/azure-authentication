using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Refit;

namespace Authentication.Shared.Services
{
    public class AnalyticsService
    {
        public interface IAnalyticsApi
        {
            /// <summary>
            /// Get access token by app client and secret
            /// </summary>
            /// <param name="tenantId">tenant id</param>
            /// <param name="data">parameter data</param>
            /// <returns>Ad user access token</returns>
            [Post("/Analytics")]
            Task<HttpResponseMessage> SendEvent([AliasAs("code")] string code, [Body(BodySerializationMethod.Serialized)] Dictionary<string, object> data);
        }

        public static AnalyticsService Instance { get; } = new AnalyticsService();
        private IAnalyticsApi analyticsApi;
        private DataService dataService;

        private AnalyticsService()
        {
            var httpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Analytics.AnalyticsUrl) };
            analyticsApi = RestService.For<IAnalyticsApi>(httpClient, new RefitSettings(new NewtonsoftJsonContentSerializer()));
            dataService = new DataService();
        }

        public async Task SendEvent(Dictionary<string, object> data)
        {
            string uuid = Guid.NewGuid().ToString();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            dynamic item = new
            {
                id = uuid,
                createdAt = timestamp,
                updatedAt = timestamp,
                type = "analytics",
                version = 1,
                eventData = new { },
                triggerAction = data,
                partition = "default",
                processed = false
            };

            await dataService.CreateDocument("Event", item);
        }

        public async Task SendEventSilent(string type, Dictionary<string, object> dict)
        {
            try
            {
                var task = SendEvent(new Dictionary<string, object>
                {

                    {
                       type, dict
                    },
                });
                await HttpHelper.TimeoutAfter(task, TimeSpan.FromSeconds(3));
            }
            catch (TimeoutException) { }
        }
    }
}
