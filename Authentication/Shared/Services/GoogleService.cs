using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services.Responses;
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

        public async Task<bool> ValidateAccessToken(string email, string accessToken)
        {
            try
            {
                Logger.Log?.LogInformation($"validate google sign in {email} {accessToken}");
                var response = await googleRestApi.ValidateAccessToken(accessToken);
                var expiredIn = int.Parse(response.Exp);
                var time = DateTime.UnixEpoch.AddSeconds(expiredIn);
                var now = DateTime.Now;
                return now < time // not expired
                    && response.Email == email // email is matched with token
                    && Configurations.Google.GoogleClientIds.Contains(response.Sub); // client id must matched
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

