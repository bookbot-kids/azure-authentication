using System;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services.Responses;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Refit;
using static Authentication.Shared.Services.CognitoService;

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

        public async Task<bool> ValidateIdToken(string idToken)
        {
            SignedTokenVerificationOptions options = new SignedTokenVerificationOptions
            {
                IssuedAtClockTolerance = TimeSpan.FromMinutes(1),
                ExpiryClockTolerance = TimeSpan.FromMinutes(1),
                TrustedAudiences = { Configurations.Google.GoogleClientId }
            };

            try
            {
                var payload = await JsonWebSignature.VerifySignedTokenAsync(idToken, options);
                return payload != null && payload.Issuer == Configurations.Google.GoogleClientId;
            }
            catch(Exception)
            {
                return false;
            }            
        }

        public async Task<bool> ValidateAccessToken(string email, string accessToken)
        {
            try
            {
                var response = await googleRestApi.ValidateAccessToken(accessToken);
                var expiredIn = int.Parse(response.Exp);
                var time = DateTime.UnixEpoch.AddSeconds(expiredIn);
                var now = DateTime.Now;
                return now < time && response.Email == email && response.Aud == Configurations.Google.GoogleClientId;
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.BadRequest)
                {
                    throw ex;
                }
            }

            return false;
        }
    }
}

