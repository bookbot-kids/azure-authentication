﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using QRCoder;
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

        public interface ISendyApi
        {
            [Post("/subscribe")]
            Task<HttpResponseMessage> Subscribe([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
        }

        public interface IBranchIOApi
        {
            [Post("/url")]
            Task<HttpResponseMessage> CreateLink([Body(BodySerializationMethod.Serialized)] Dictionary<string, object> data);
        }

        public static AnalyticsService Instance { get; } = new AnalyticsService();
        private IAnalyticsApi analyticsApi;
        private ISendyApi sendyApi;
        private IBranchIOApi branchIOApi;
        private DataService dataService;

        private AnalyticsService()
        {
            var analyticsHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Analytics.AnalyticsUrl) };
            analyticsApi = RestService.For<IAnalyticsApi>(analyticsHttpClient, new RefitSettings(new NewtonsoftJsonContentSerializer()));
            dataService = new DataService();
        }

        private void InitSendy()
        {
            if (sendyApi == null)
            {
                var sendyHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Analytics.SendyUrl) };
                sendyApi = RestService.For<ISendyApi>(sendyHttpClient, new RefitSettings(new NewtonsoftJsonContentSerializer()));
            }
        }

        private void InitBranchIO()
        {
            if (branchIOApi == null)
            {
                var branchIOHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri("https://api2.branch.io/v1") };
                branchIOApi = RestService.For<IBranchIOApi>(branchIOHttpClient, new RefitSettings(new NewtonsoftJsonContentSerializer()));
            }
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

        public async Task SubscribeToSendyList(string listId, string email, string name = "", string ipAddress = "",
            string offer = "", string qrcode = "", string role = "", string language = "", string country = "", string os = "")
        {
            InitSendy();
            var data = new Dictionary<string, object> {
                {"api_key", Configurations.Analytics.SendyKey},
                {"email", email},
                {"list", listId},
            };

            if (!string.IsNullOrWhiteSpace(name))
            {
                data["name"] = name;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                data["ipaddress"] = ipAddress;
            }

            if (!string.IsNullOrWhiteSpace(offer))
            {
                data["Offer"] = offer;
            }

            if (!string.IsNullOrWhiteSpace(qrcode))
            {
                data["QRCode"] = qrcode;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                data["Role"] = role;
            }

            if (!string.IsNullOrWhiteSpace(language))
            {
                data["Language"] = language;
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                data["country"] = country.ToUpper();
            }

            if (!string.IsNullOrWhiteSpace(os))
            {
                data["OS"] = os;
            }

            await sendyApi.Subscribe(data);
        }

        public async Task<string> CreateDeepLink(Dictionary<string, object> data)
        {
            InitBranchIO();
            var response = await branchIOApi.CreateLink(data);
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonObject = JObject.Parse(jsonString);
            string url = jsonObject["url"].ToString();
            return url;
        }

        /// <summary>
        /// Subscribe user to sendy list.
        /// If subscribe list not defined, it will use registered list
        /// </summary>
        /// <param name="subscribeList"></param>
        /// <param name="email"></param>
        /// <param name="name"></param>
        /// <param name="ipAddress"></param>
        /// <param name="appId"></param>
        /// <param name="language"></param>
        /// <param name="country"></param>
        /// <param name="os"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task SubscribeToEmailList(
            string subscribeList, string email, string name,
            string ipAddress, string appId, string language, string country, string os, string role)
        {
            // create deeplink and add to sendy
            var expiry = DateTime.Now.AddDays(35).ToString("yyyy-MM-dd HH:mm:ss.fff");
            var branchIOKey = Configurations.Configuration[$"BranchIOKey_{appId}"];
            if (!string.IsNullOrWhiteSpace(branchIOKey))
            {
                var data = new Dictionary<string, object>
                        {
                             {"branch_key", branchIOKey},
                             {"channel", Configurations.Apple.AppleServiceId},
                             {"feature", "email"},
                             {"campaign", "offer"},
                             {"tags", new List<string> { "Discount" } },
                             {"data", new Dictionary<string, object> {
                                 {"offer", Configurations.Configuration["OfferPackage"]},
                                 {"$canonical_url", Configurations.JWTToken.TokenIssuer},
                                 {"expiry", expiry},

                             } },
                        };

                var deepLink = await AnalyticsService.Instance.CreateDeepLink(data);

                // create QRCode image
                var filename = Guid.NewGuid().ToString() + ".png";
                var qrcodeUrl = $"{Configurations.Configuration["CloudFlareR2Url"]}/qrcode/{filename}";

                Logger.Log?.LogInformation($"Generate link {deepLink}, qrcode url {qrcodeUrl} for user ${email}");
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(10);
                    // upload qrcode to cloudflare r2
                    await StorageService.UpdateToCloudFlareR2(Configurations.Configuration["CloudFlareR2Key"], qrcodeUrl, qrCodeImage, "image/png");

                    // Send to sendy
                    await AnalyticsService.Instance.SubscribeToSendyList(subscribeList, email, name: name,
                        ipAddress: ipAddress, offer: deepLink, qrcode: qrcodeUrl, language: language, country: country, os: os, role: role);
                }
            }
            else
            {
                Logger.Log?.LogError($"Ignore invalid appid {appId}");
            }
        }
    }
}
