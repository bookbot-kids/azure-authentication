﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Extensions;
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

            [Post("/api/subscribers/delete.php")]
            Task<HttpResponseMessage> DeleteSubscriber([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
        }

        public interface IBranchIOApi
        {
            [Post("/url")]
            Task<HttpResponseMessage> CreateLink([Body(BodySerializationMethod.Serialized)] Dictionary<string, object> data);
        }

        public interface IReoonApi
        {
            [Get("/api/v1/verify")]
            Task<HttpResponseMessage> Verify([Query("email")]  string email, [Query("key")] string key);
        }

        public static AnalyticsService Instance { get; } = new AnalyticsService();
        private IAnalyticsApi analyticsApi;
        private ISendyApi sendyApi;
        private IBranchIOApi branchIOApi;
        private IReoonApi reoonApi;
        private DataService dataService;

        private AnalyticsService()
        {
            var analyticsHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Analytics.AnalyticsUrl) };
            analyticsApi = RestService.For<IAnalyticsApi>(analyticsHttpClient, new RefitSettings(new NewtonsoftJsonContentSerializer()));
            dataService = new DataService();

            reoonApi = RestService.For<IReoonApi>(new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri("https://emailverifier.reoon.com") }, new RefitSettings(new NewtonsoftJsonContentSerializer()));
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

        public async Task DeleteFromSendyList(string listId, string email)
        {
            InitSendy();
            await HttpHelper.ExecuteWithRetryAsync(async () =>
            {
                var data = new Dictionary<string, object> {
                    {"api_key", Configurations.Analytics.SendyKey},
                    {"email", email},
                    {"list_id", listId},
                };
                await sendyApi.DeleteSubscriber(data);
            }, comment: "delete from sendy");
        }

        public async Task<string> ValidateEmailStatus(string email)
        {
            var status = await HttpHelper.ExecuteWithRetryAsync(async () =>
            {
                var response = await reoonApi.Verify(email, Configurations.Analytics.ReoonKey);
                string jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonString);
                string status = jsonObject["status"]?.ToString();
                return status;
            }, comment: "Verify email");
            return status;
        }

        public async Task SubscribeToSendyList(string listId, string email, string name = "", string ipAddress = "",
            string offer = "", string qrcode = "", string userType = "", string language = "", string country = "", string os = "",
            string inviteEducatorName = "", string inviteUrl = "", string inviteChildFirstName = "", string inviteChildLastName = "", bool ignoreSpamEmailCheck = false)
        {
            if (StringHelper.IsTestEmail(Configurations.AzureB2C.EmailTestDomain, email))
            {
                Logger.Log?.LogWarning($"ignore test email {email}");
                return;
            }

            if (!ignoreSpamEmailCheck)
            {
                // validate email status
                var status = await ValidateEmailStatus(email);

                if (status != "valid")
                {
                    Logger.Log?.LogWarning($"ignore {email} with invalid status {status}");
                    return;
                }
            }            

            InitSendy();
            var data = new Dictionary<string, object> {
                {"api_key", Configurations.Analytics.SendyKey},
                {"email", email},
                {"list", listId},
            };

            if (!string.IsNullOrWhiteSpace(name))
            {
                // always send first name into sendy
                data["name"] = StringHelper.ExtractFirstName(name);
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

            if (!string.IsNullOrWhiteSpace(userType))
            {
                data["UserType"] = userType;
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

            if (!string.IsNullOrWhiteSpace(inviteEducatorName))
            {
                data["EducatorName"] = inviteEducatorName;
            }

            if (!string.IsNullOrWhiteSpace(inviteChildFirstName))
            {
                data["ChildFirstName"] = inviteChildFirstName;
            }

            if (!string.IsNullOrWhiteSpace(inviteChildLastName))
            {
                data["ChildLastName"] = inviteChildLastName;
            }

            if (!string.IsNullOrWhiteSpace(inviteUrl))
            {
                data["URL"] = inviteUrl;
            }

            await HttpHelper.ExecuteWithRetryAsync(async () =>
            {
                await sendyApi.Subscribe(data);
            }, comment: "Subscribe sendy");
        }

        public async Task<string> CreateDeepLink(Dictionary<string, object> data)
        {
            InitBranchIO();

            return await HttpHelper.ExecuteWithRetryAsync(async () =>
            {
                var response = await branchIOApi.CreateLink(data);
                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonString);
                string url = jsonObject["url"].ToString();
                return url;
            }, comment: "CreateDeepLink");
        }

        public async Task<string> CreateOfferLink(string appId, string prefixKey = "BranchIOKey", int days = 10)
        {
            var expiry = DateTime.Now.AddDays(days).ToString("yyyy-MM-dd HH:mm:ss.fff");
            var branchIOKey = Configurations.Configuration[$"{prefixKey}_{appId}"];
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
                                 {"$desktop_deepview",  Configurations.Configuration["BranchIODeepView"]}
                             } },
                        };

                var deepLink = await CreateDeepLink(data);
                return deepLink;
            }

            return null;
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
        public async Task<string> SubscribeNewUser(
           string email, string name,
            string ipAddress, string appId, string language, string country, string os, string userType)
        {
            // create deeplink and add to sendy
            var deepLink = await CreateOfferLink(appId);
            if (!string.IsNullOrWhiteSpace(deepLink))
            {
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
                    await HttpHelper.ExecuteWithRetryAsync(async () =>
                    {
                        await StorageService.UpdateToCloudFlareR2(Configurations.Configuration["CloudFlareR2Key"], qrcodeUrl, qrCodeImage, "image/png");
                    }, comment: "Upload qrcode to cloudflare");                    

                    // Send to sendy registered and tips list
                    await SubscribeToSendyList(Configurations.Analytics.SendyRegisteredListId, email, name: name,
                        ipAddress: ipAddress, offer: deepLink, qrcode: qrcodeUrl, language: language, country: country, os: os, userType: userType);
                    await SubscribeToSendyList(Configurations.Analytics.SendyTipsListId, email, name: name,
                       ipAddress: ipAddress, offer: deepLink, qrcode: qrcodeUrl, language: language, country: country, os: os, userType: userType);
                    return qrcodeUrl;
                }
            }
            else
            {
                Logger.Log?.LogError($"Ignore invalid appid {appId}");
            }

            return null;
        }

        public async Task<string> GenerateQRCode2Cloud(string deepLink, string imagePath = "qrcode", string name = null)
        {
            var filename = name ?? Guid.NewGuid().ToString() + ".png";
            var qrcodeUrl = $"{Configurations.Configuration["CloudFlareR2Url"]}/{imagePath}/{filename}";
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                
                byte[] qrCodeImage = qrCode.GetGraphic(10);
                // upload qrcode to cloudflare r2
                await HttpHelper.ExecuteWithRetryAsync(async () =>
                {
                    await StorageService.UpdateToCloudFlareR2(Configurations.Configuration["CloudFlareR2Key"], qrcodeUrl, qrCodeImage, "image/png");
                }, comment: "Upload qrcode to cloudflare");
            }

            return qrcodeUrl;
        }

        public async Task GenerateQRCode(string deepLink, string filePath)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {

                byte[] qrCodeImage = qrCode.GetGraphic(10);
                await File.WriteAllBytesAsync(filePath, qrCodeImage);
            }
        }

        public async Task SendSlackMessageAsync(string webhookUrl, string message)
        {
            using (HttpClient client = new HttpClient())
            {
                string json = $"{{\"text\":\"{message}\"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(webhookUrl, content);
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to send message to Slack. Status: {response.StatusCode}, Response: {errorContent}");
                }
            }
        }
    }
}
