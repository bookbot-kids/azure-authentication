using System;
using Authentication.Shared.Library;
using Authentication.Shared.Services.Responses;
using Refit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Amazon.Runtime.Internal.Transform;
using Azure.Core;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Services
{
    public class AppleService
    {
        public interface IAppleRestApi
        {
            [Post("/token")]
            Task<AppleTokenResponse> ValidateIdToken([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
        }
        private AppleService()
        {
            appleRestApi = RestService.For<IAppleRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://appleid.apple.com/auth")
            });
        }

        public static AppleService Instance { get; } = new AppleService();
        private IAppleRestApi appleRestApi;

        private string GenerateSecretToken()
        {
            return TokenService.GenerateAppleToken(Configurations.Apple.AppleSecret, Configurations.Apple.AppleClientId, Configurations.Apple.AppleAppId,
                Configurations.Apple.AppleTeamId, "https://appleid.apple.com", DateTime.UtcNow.AddDays(1));
        }

        public async Task<(bool, string)> ValidateToken(string email, string authCode, string idToken)
        {
            Logger.Log?.LogInformation($"validate apple sign in {email} {authCode} {idToken}");
            var isValid = TokenService.ValidatePublicJWTToken(idToken, new Dictionary<string, string>
            {
                {"email", email },
                {"iss", "https://appleid.apple.com" },
                {"aud", Configurations.Apple.AppleAppId },

            });

            if(!isValid.Item1)
            {
                return (false, "Id token is invalid");
            }

            var secret = GenerateSecretToken();
            try
            {
                var response = await appleRestApi.ValidateIdToken(new Dictionary<string, object>
                {
                    {"client_id", Configurations.Apple.AppleAppId },
                    {"client_secret", secret },
                    {"code", authCode },
                    {"grant_type", "authorization_code" },
                    {"redirect_uri", Configurations.Apple.AppleRedirectUrl },
                });

                return (!string.IsNullOrWhiteSpace(response.AccessToken), "");
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.BadRequest)
                {
                    throw ex;
                }
            }

            return (false, "Auth code is invalid");
        }
    }
}

