using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Refit;

namespace Authentication.Shared.Services
{
    public class CognitoService
    {
        public interface ICognitoRestApi
        {
            [Headers("Content-Type: application/x-www-form-urlencoded")]
            [Post("/oauth2/token?grant_type=refresh_token")]
            Task<ADToken> GetAccessToken([AliasAs("client_id")] string clientId, [AliasAs("refresh_token")] string refreshToken);
        }

        private readonly HttpClient httpClient;
        private ICognitoRestApi cognitoRestApi;
        public static CognitoService Instance { get; } = new CognitoService();
        private CognitoService()
        {
            httpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Cognito.CognitoUrl) };
            cognitoRestApi = RestService.For<ICognitoRestApi>(httpClient);
        }
        

        public async Task<ADToken> GetAccessToken(string refreshToken)
        {
            return await cognitoRestApi.GetAccessToken(Configurations.Cognito.CognitoClientId, refreshToken);
        }

        public async Task<(bool, string, string, string)> ValidateAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "token is missing", null, null);
            }

            var claimsIdentity = await TokenService.ValidateCognitoToken(token);
            if (claimsIdentity != null)
            {
                var idClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("username"));
                var groupClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("cognito:groups"));
                if (idClaim != null && groupClaim != null)
                {
                    return (true, null, idClaim.Value, groupClaim.Value);
                }

                return (false, "Can not find user with this token", null, null);
            }

            return (false, "Token is invalid", null, null);
        }
    }
}

