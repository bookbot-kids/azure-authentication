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

        private AnalyticsService()
        {
            var httpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Analytics.AnalyticsUrl) };
            analyticsApi = RestService.For<IAnalyticsApi>(httpClient);
        }

        public async Task SendEvent(Dictionary<string, object> data)
        {
            var body = new Dictionary<string, object>
            {
                {"triggerAction", data }
            };
            var response = await analyticsApi.SendEvent(Configurations.Analytics.AnalyticsToken, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
