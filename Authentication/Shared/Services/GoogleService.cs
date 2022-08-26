using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services.Responses;
using Extensions;
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
        private GoogleService()
        {
            googleRestApi = RestService.For<IGoogleRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://www.googleapis.com/oauth2/v3")
            });
        }

        public static GoogleService Instance { get; } = new GoogleService();
        private IGoogleRestApi googleRestApi;

        public async Task<(bool, string)> ValidateAccessToken(string email, string accessToken, string idToken)
        {
            if(!string.IsNullOrWhiteSpace(idToken))
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
                }
            }

            try
            {
                Logger.Log?.LogInformation($"validate google sign in {email} {accessToken}");
                var response = await googleRestApi.ValidateAccessToken(accessToken);
                var expiredIn = int.Parse(response.Exp);
                var time = DateTime.UnixEpoch.AddSeconds(expiredIn);
                var now = DateTime.Now;
                var isAccessTokenValid = now < time // not expired
                    && response.Email == email // email is matched with token
                    && Configurations.Google.GoogleClientIds.Contains(response.Aud); // client id must matched
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
    }
}

