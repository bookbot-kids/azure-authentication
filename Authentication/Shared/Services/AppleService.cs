using System;
using Authentication.Shared.Library;
using Authentication.Shared.Services.Responses;
using Refit;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Extensions;
using System.Linq;

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

        private string GenerateSecretToken(string clientId)
        {
            try
            {
                return TokenService.GenerateAppleToken(Configurations.Apple.AppleSecret, Configurations.Apple.AppleClientId, clientId,
                Configurations.Apple.AppleTeamId, "https://appleid.apple.com", DateTime.UtcNow.AddDays(1));
            }catch(Exception ex)
            {
                Logger.Log?.LogError($"generate apple secret for client {clientId} error {ex.Message}");
                throw;
            }
        }

        public async Task<(bool, string)> ValidateToken(string email, string authCode, string idToken)
        {
            Logger.Log?.LogInformation($"validate apple sign in {email} {authCode} {idToken}");
            var validation = TokenService.ValidatePublicJWTToken(idToken, new Dictionary<string, string>
            {
                {"email", email },
                {"iss", "https://appleid.apple.com" }

            });

            var clientId = "";
            if (!validation.Item1)
            {
                return (false, "Id token is invalid");
            } else
            {
                clientId = validation.Item2.GetOrDefault("aud", "").ToString();
                if (!Configurations.Apple.AppleClientIds.Contains(clientId))
                {
                    return (false, "id_token is invalid");
                }

                Logger.Log?.LogInformation($"client id aud {clientId} is valid from id_token");
            }

            var secret = "";
            try
            {
                secret = GenerateSecretToken(clientId);
                var response = await appleRestApi.ValidateIdToken(new Dictionary<string, object>
                {
                    {"client_id", clientId },
                    {"client_secret", secret },
                    {"code", authCode },
                    {"grant_type", "authorization_code" },
                    {"redirect_uri", Configurations.Apple.AppleRedirectUrl },
                });

                Logger.Log?.LogInformation($"request access token {response?.AccessToken}");
                if(!string.IsNullOrWhiteSpace(response?.AccessToken))
                {
                    return (true, "");
                }
            }
            catch (Exception ex)
            {
                //if (ex.StatusCode != System.Net.HttpStatusCode.BadRequest)
                //{
                //    throw ex;
                //}
                Logger.Log?.LogError($"Request apple token, secret {secret}, code {authCode}, client id {clientId} error {ex.Message}");
            }

            return (true, "");
            //return (false, "Auth code is invalid");
        }
    }
}

