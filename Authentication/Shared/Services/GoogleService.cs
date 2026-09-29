using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Services.Responses;
using Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Refit;

namespace Authentication.Shared.Services
{
    public class GoogleService
    {
        public interface IGoogleRestApi
        {
            [Get("/tokeninfo")]
            Task<GoogleTokenResponse> ValidateAccessToken([AliasAs("access_token")] string accessToken);
        }

        public interface IFirebaseRestApi
        {
            [Post("/shortLinks")]
            Task<DeepLink> GenerateShortLink([AliasAs("key")] string key, [Body(BodySerializationMethod.Serialized)] ExpandoObject body);
        }

        private GoogleService() : this(
            RestService.For<IGoogleRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://www.googleapis.com/oauth2/v3")
            }),
            RestService.For<IFirebaseRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://firebasedynamiclinks.googleapis.com/v1")
            }))
        {
        }

        internal GoogleService(IGoogleRestApi googleRestApi, IFirebaseRestApi firebaseRestApi)
        {
            this.googleRestApi = googleRestApi;
            this.firebaseRestApi = firebaseRestApi;
        }

        public static GoogleService Instance { get; } = new GoogleService();
        private readonly IGoogleRestApi googleRestApi;
        private readonly IFirebaseRestApi firebaseRestApi;

        /// <summary>
        /// Validate Google sign in: the id token (when sent) must be signed by Google for this email and one of our
        /// client ids, and Google must confirm the access token belongs to this email and to our Google project
        /// </summary>
        public async Task<(bool, string)> ValidateAccessToken(string email, string accessToken, string idToken)
        {
            Logger.Log?.LogInformation($"validate google sign in {email}");
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                var (valid, payload) = await TokenService.ValidateIdToken(idToken, email, TokenService.OpenIdProvider.Google);
                if (!valid)
                {
                    return (false, "id_token is invalid");
                }

                // claim client id
                var aud = payload.GetOrDefault("aud", "").ToString();
                if (!Configurations.Google.GoogleClientIds.Contains(aud))
                {
                    return (false, "id_token is invalid");
                }

                Logger.Log?.LogInformation($"client id aud {aud} is valid from id_token");
            }

            try
            {               
                var response = await googleRestApi.ValidateAccessToken(accessToken);
                var expiredIn = int.Parse(response.Exp);
                var time = DateTime.UnixEpoch.AddSeconds(expiredIn);
                var now = DateTime.UtcNow;
                Logger.Log?.LogInformation($"validate access token google sign aud {response.Aud}, email {response.Email}");
                var isAccessTokenValid = now < time // not expired
                    && response.Email == email // email is matched with token
                    && IsOurGoogleClient(response.Aud); // issued to one of our apps, not another app's token
                if(isAccessTokenValid)
                {
                    return (isAccessTokenValid, "");
                }
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.BadRequest)
                {
                    throw ex;
                }
            }

            return (false, "access_token is invalid");
        }

        /// <summary>
        /// Access tokens can be issued to OAuth clients that are not in GoogleClientIds (e.g. the iOS/Android client
        /// of the same app), so accept any client of the Google Cloud projects our configured client ids belong to
        /// </summary>
        internal static bool IsOurGoogleClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return false;
            }

            var clientIds = Configurations.Google.GoogleClientIds;
            if (clientIds.Contains(clientId))
            {
                return true;
            }

            var project = GoogleProjectNumber(clientId);
            return project != null && clientIds.Any(id => GoogleProjectNumber(id) == project);
        }

        // "<project>-<id>.apps.googleusercontent.com" or "com.googleusercontent.apps.<project>-<id>"
        private static string GoogleProjectNumber(string clientId)
        {
            var match = Regex.Match(clientId, @"^(?:com\.googleusercontent\.apps\.)?(\d+)-[0-9a-z]+(?:\.apps\.googleusercontent\.com)?$");
            return match.Success ? match.Groups[1].Value : null;
        }

        public async Task<DeepLink> GenerateDynamicLink(string key, string domain, string androidPackage, string iosPackage, string iosAppId, Dictionary<string, string> parameters)
        {
            var url = QueryHelpers.AddQueryString($"{domain}/p", parameters);
            dynamic body = new ExpandoObject();
            body.dynamicLinkInfo = new ExpandoObject();
            body.dynamicLinkInfo.domainUriPrefix = domain;
            body.dynamicLinkInfo.link = url;
            body.dynamicLinkInfo.androidInfo = new ExpandoObject();
            body.dynamicLinkInfo.androidInfo.androidPackageName = androidPackage;
            body.dynamicLinkInfo.iosInfo = new ExpandoObject();
            body.dynamicLinkInfo.iosInfo.iosBundleId = iosPackage;
            body.dynamicLinkInfo.iosInfo.iosAppStoreId = iosAppId;
            body.dynamicLinkInfo.socialMetaTagInfo = new ExpandoObject();
            body.dynamicLinkInfo.socialMetaTagInfo.socialTitle = "Bookbot";
            return await firebaseRestApi.GenerateShortLink(key, body);
        }
    }
}

