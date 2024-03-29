﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
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

        private GoogleService()
        {
            googleRestApi = RestService.For<IGoogleRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://www.googleapis.com/oauth2/v3")
            });

            firebaseRestApi = RestService.For<IFirebaseRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://firebasedynamiclinks.googleapis.com/v1")
            });
        }

        public static GoogleService Instance { get; } = new GoogleService();
        private IGoogleRestApi googleRestApi;
        private IFirebaseRestApi firebaseRestApi;

        public async Task<(bool, string)> ValidateAccessToken(string email, string accessToken, string idToken)
        {
            Logger.Log?.LogInformation($"validate google sign in {email} {accessToken} {idToken}");
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                var validation = TokenService.ValidatePublicJWTToken(idToken, new Dictionary<string, string>
                {
                    {"email", email },
                    {"iss", "https://accounts.google.com" },

                });

                if(!validation.Item1)
                {
                    return (false, "id_token is invalid");
                } else
                {
                    // claim client id
                    var aud = validation.Item2.GetOrDefault("aud", "").ToString();
                    if(!Configurations.Google.GoogleClientIds.Contains(aud))
                    {
                        return (false, "id_token is invalid");
                    }

                    Logger.Log?.LogInformation($"client id aud {aud} is valid from id_token");
                }
            }

            try
            {               
                var response = await googleRestApi.ValidateAccessToken(accessToken);
                var expiredIn = int.Parse(response.Exp);
                var time = DateTime.UnixEpoch.AddSeconds(expiredIn);
                var now = DateTime.Now;
                Logger.Log?.LogInformation($"validate access token google sign aud {response.Aud}, email {response.Email}");
                var isAccessTokenValid = now < time // not expired
                    && response.Email == email; // email is matched with token
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

