using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Refit;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Microsoft Service
    /// A singleton class that accesses the Microsoft APIs, uses to get or refresh admin token to run operations (CRUD ADUser) without user authenticate <br/>
    /// Ref: <see cref="https://docs.microsoft.com/en-us/graph/auth-v2-service#authentication-and-authorization-steps"/>
    /// </summary>
    public class MicrosoftService
    {
        /// <summary>
        /// Rest api service
        /// </summary>
        private readonly IMicrosoftRestApi service;

        /// <summary>
        /// Http client
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Prevents a default instance of the <see cref="MicrosoftService"/> class from being created
        /// </summary>
        private MicrosoftService()
        {
            httpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.AzureB2C.MicorsoftAuthUrl) };
            service = RestService.For<IMicrosoftRestApi>(httpClient);
        }

        /// <summary>
        /// Microsoft online service
        /// This interface contains the defined rest APIs of Microsoft online service
        /// </summary>
        public interface IMicrosoftRestApi
        {
            /// <summary>
            /// Get access token by app client and secret
            /// </summary>
            /// <param name="tenantId">tenant id</param>
            /// <param name="data">parameter data</param>
            /// <returns>Ad user access token</returns>
            [Headers("Content-Type: application/x-www-form-urlencoded")]
            [Post("/{tenantId}/oauth2/token")]
            Task<ADToken> GetAccessToken([AliasAs("tenantId")] string tenantId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

            /// <summary>
            /// Get master token by app client and secret
            /// </summary>
            /// <param name="tenantId">tenant id</param>
            /// <param name="data">parameter data</param>
            /// <returns>Ad user access token</returns>
            [Headers("Content-Type: application/x-www-form-urlencoded")]
            [Post("/{tenantId}/oauth2/token")]
            Task<ADToken> GetMasterToken([AliasAs("tenantId")] string tenantId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

            /// <summary>
            /// Refresh token
            /// </summary>
            /// <param name="tenantId">tenant id</param>
            /// <param name="data">parameter data</param>
            /// <returns>Ad user access token</returns>
            [Headers("Content-Type: application/x-www-form-urlencoded")]
            [Post("/{tenantId}/oauth2/token")]
            Task<ADToken> RefreshToken([AliasAs("tenantId")] string tenantId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
        }

        /// <summary>
        /// Gets singleton instance
        /// </summary>
        public static MicrosoftService Instance { get; } = new MicrosoftService();

        /// <summary>
        /// Get access token for admin
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="password">password param</param>
        /// <returns><see cref="ADToken" /> class</returns>
        public Task<ADToken> GetAdminAccessToken(string email, string password)
        {
            var parameters = new Dictionary<string, object>
            {
                { "grant_type", Configurations.AzureB2C.GrantTypePassword },
                { "client_id", Configurations.AzureB2C.AdminClientId },
                { "client_secret", Configurations.AzureB2C.AdminClientSecret },
                { "resource", Configurations.AzureB2C.GraphResource },
                { "username", email },
                { "password", password }
            };

            return service.GetAccessToken(Configurations.AzureB2C.TenantId, parameters);
        }

        /// <summary>
        /// Get master token
        /// </summary>
        /// <returns><see cref="ADToken" /> class</returns>
        public Task<ADToken> GetMasterToken()
        {
            var parameters = new Dictionary<string, object>
            {
                { "grant_type", Configurations.AzureB2C.GrantTypeCredentials },
                { "client_id", Configurations.AzureB2C.AdminClientId },
                { "client_secret", Configurations.AzureB2C.AdminClientSecret },
                { "resource", Configurations.AzureB2C.GraphResource },
                { "scope", $"{Configurations.AzureB2C.GraphResource}/.default" }
            };

            return service.GetAccessToken(Configurations.AzureB2C.TenantId, parameters);
        }

        /// <summary>
        /// Refresh a token for admin
        /// </summary>
        /// <param name="refreshToken">refresh token</param>
        /// <returns>New ADToken</returns>
        public Task<ADToken> RefreshAdminAccessToken(string refreshToken)
        {
            var parameters = new Dictionary<string, object>
            {
                { "grant_type", Configurations.AzureB2C.GrantTypeRefreshToken },
                { "client_id", Configurations.AzureB2C.AdminClientId },
                { "client_secret", Configurations.AzureB2C.AdminClientSecret },
                { "resource", Configurations.AzureB2C.GraphResource },
                { "refresh_token", refreshToken }
            };

            return service.RefreshToken(Configurations.AzureB2C.TenantId, parameters);
        }
    }
}
