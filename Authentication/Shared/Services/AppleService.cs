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
        private AppleService() : this(RestService.For<IAppleRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri("https://appleid.apple.com/auth")
            }, new RefitSettings(new NewtonsoftJsonContentSerializer())))
        {
        }

        internal AppleService(IAppleRestApi appleRestApi)
        {
            this.appleRestApi = appleRestApi;
        }

        public static AppleService Instance { get; } = new AppleService();
        private readonly IAppleRestApi appleRestApi;

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

        /// <summary>
        /// Validate Sign in with Apple: the id token must be signed by Apple for this email and one of our client ids,
        /// and Apple must accept the authorization code
        /// </summary>
        /// <param name="redirectUri">
        /// Redirect URI the code was issued to, sent by web sign-in; the configured Cognito URI otherwise (apps).
        /// Apple only redeems a code with the exact URI it was issued for.
        /// </param>
        public async Task<(bool, string)> ValidateToken(string email, string authCode, string idToken, string redirectUri = null)
        {
            Logger.Log?.LogInformation($"validate apple sign in {email}");
            if (!string.IsNullOrWhiteSpace(redirectUri)
                && !(Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && redirectUri.Length <= 2048))
            {
                return (false, "redirect_uri is invalid");
            }

            var (valid, payload) = await TokenService.ValidateIdToken(idToken, email, TokenService.OpenIdProvider.Apple);
            if (!valid)
            {
                return (false, "Id token is invalid");
            }

            var clientId = payload.GetOrDefault("aud", "").ToString();
            if (!Configurations.Apple.AppleClientIds.Contains(clientId))
            {
                return (false, "id_token is invalid");
            }

            Logger.Log?.LogInformation($"client id aud {clientId} is valid from id_token");

            try
            {
                var response = await appleRestApi.ValidateIdToken(new Dictionary<string, object>
                {
                    {"client_id", clientId },
                    {"client_secret", GenerateSecretToken(clientId) },
                    {"code", authCode },
                    {"grant_type", "authorization_code" },
                    {"redirect_uri", string.IsNullOrWhiteSpace(redirectUri) ? Configurations.Apple.AppleRedirectUrl : redirectUri },
                });

                var redeemed = !string.IsNullOrWhiteSpace(response?.AccessToken);
                Logger.Log?.LogInformation($"request access token issued {redeemed}");
                if (redeemed)
                {
                    return (true, "");
                }
            }
            catch (ApiException ex)
            {
                Logger.Log?.LogError($"Request apple token for client {clientId} error {ex.Message} {ex.Content}");
            }
            catch (Exception ex)
            {
                Logger.Log?.LogError($"Request apple token for client {clientId} error {ex.Message}");
            }

            return (false, "Auth code is invalid");
        }
    }
}

