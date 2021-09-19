using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Microsoft.Extensions.Logging;
using Refit;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Azure graph rest api
    /// This interface contains the defined rest APIs of azure graph
    /// </summary>
    public interface IAzureGraphRestApi
    {
        /// <summary>
        /// Get AD User by id
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="accessToken">Access token</param>
        /// <returns>ADUser class</returns>
        [Headers("Accept: application/json")]
        [Get("/{tenantId}/users/{userId}?api-version=1.6")]
        Task<ADUser> GetUserById([AliasAs("tenantId")] string tenantId, [AliasAs("userId")] string userId, [Header("Authorization")] string accessToken);

        /// <summary>
        /// Check if user is in a group
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="param">parameters value</param>
        /// <returns>APIResult class</returns>
        [Headers("Accept: application/json")]
        [Get("/{tenantId}/isMemberOf?api-version=1.6")]
        Task<APIResult> IsMemberOf([AliasAs("tenantId")] string tenantId, [Header("Authorization")] string accessToken, [Body(BodySerializationMethod.Serialized)] IsMemberOfParam param);

        /// <summary>
        /// Create AD user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="param">parameters value</param>
        /// <returns>Created ADUser</returns>
        [Headers("Accept: application/json")]
        [Post("/{tenantId}/users?api-version=1.6")]
        Task<ADUser> CreateUser([AliasAs("tenantId")] string tenantId, [Header("Authorization")] string accessToken, [Body(BodySerializationMethod.Serialized)] CreateADUserParameters param);

        /// <summary>
        /// Update AD user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="param">parameters value</param>
        /// <returns>Created ADUser</returns>
        [Headers("Accept: application/json")]
        [Patch("/{tenantId}/users/{userId}?api-version=1.6")]
        Task<ADUser> UpdateUser([AliasAs("tenantId")] string tenantId, [AliasAs("userId")] string userId, [Header("Authorization")] string accessToken, [Body(BodySerializationMethod.Serialized)] Dictionary<string, string> param);

        /// <summary>
        /// Add user into a group
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="groupId">Group id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="parameters">parameters value</param>
        /// <returns>API Result</returns>
        [Headers("Accept: application/json")]
        [Post("/{tenantId}/groups/{groupId}/$links/members?api-version=1.6")]
        Task<APIResult> AddUserToGroup([AliasAs("tenantId")] string tenantId, [AliasAs("groupId")] string groupId, [Header("Authorization")] string accessToken, [Body(BodySerializationMethod.Serialized)] AddUserToGroupParameter parameters);

        /// <summary>
        /// Search users
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="query">search query</param>
        /// <returns>Search result</returns>
        [Headers("Accept: application/json")]
        [Get("/{tenantId}/users?api-version=1.6&$filter={query}")]
        Task<SearchUserResponse> SearchUser([AliasAs("tenantId")] string tenantId, [Header("Authorization")] string accessToken, [AliasAs("query")] string query);

        /// <summary>
        /// Get groups of user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="accessToken">access token</param>
        /// <param name="parameters">parameters value</param>
        /// <returns>List of groups</returns>
        [Headers("Accept: application/json")]
        [Get("/{tenantId}/users/{userId}/getMemberGroups?api-version=1.6")]
        Task<UserGroupsResponse> GetUserGroups([AliasAs("tenantId")] string tenantId, [AliasAs("userId")] string userId, [Header("Authorization")] string accessToken, [Body(BodySerializationMethod.Serialized)] Dictionary<string, object> parameters);

        /// <summary>
        /// Remove user from a group
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="groupId">Group id</param>
        /// <param name="accessToken">access token</param>
        /// <returns>Api result</returns>
        [Headers("Accept: application/json")]
        [Delete("/{tenantId}/groups/{groupId}/$links/members/{userId}/?api-version=1.6")]
        Task<APIResult> RemoveUserFromGroup([AliasAs("tenantId")] string tenantId, [AliasAs("userId")] string userId, [AliasAs("groupId")] string groupId, [Header("Authorization")] string accessToken);

        /// <summary>
        /// Get all groups of tenant
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="accessToken">access token</param>
        /// <returns>Group response</returns>
        [Headers("Accept: application/json")]
        [Get("/{tenantId}/groups?api-version=1.6")]
        Task<GroupsResponse> GetAllGroups([AliasAs("tenantId")] string tenantId, [Header("Authorization")] string accessToken);
    }

    /// <summary>
    /// B2C rest api
    /// This interface contains the defined rest APIs of b2c
    /// </summary>
    public interface IB2CRestApi
    {
        /// <summary>
        /// Get access token
        /// </summary>
        /// <param name="parameters">Parameters dictionary</param>
        /// <returns>ADToken class</returns>
        [Headers("Accept: application/json")]
        [Post("/token")]
        Task<ADToken> GetAccessToken([Query] Dictionary<string, object> parameters);

        /// <summary>
        /// Refresh token
        /// </summary>
        /// <param name="parameters">Parameters dictionary</param>
        /// <returns>ADToken class</returns>
        [Headers("Accept: application/json")]
        [Post("/token")]
        Task<ADToken> RefreshToken([Query] Dictionary<string, object> parameters);
    }

    /// <summary>
    /// B2C Azure service
    /// A singleton helper to call b2c and graph APIs
    /// </summary>
    public sealed class AzureB2CService
    {
        /// <summary>
        /// Azure graph service
        /// </summary>
        private readonly IAzureGraphRestApi azureGraphRestApi;

        /// <summary>
        /// B2C service
        /// </summary>
        private readonly IB2CRestApi b2cRestApi;

        /// <summary>
        /// Azure graph Http client
        /// </summary>
        private readonly HttpClient graphHttpClient;

        /// <summary>
        /// B2C Http client
        /// </summary>
        private readonly HttpClient b2cHttpClient;

        /// <summary>
        /// Prevents a default instance of the <see cref="AzureB2CService" /> class from being created
        /// </summary>
        private AzureB2CService()
        {
            graphHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.AzureB2C.GraphResource) };
            azureGraphRestApi = RestService.For<IAzureGraphRestApi>(graphHttpClient);

            b2cHttpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.AzureB2C.B2CUrl) };
            b2cRestApi = RestService.For<IB2CRestApi>(b2cHttpClient);
        }

        /// <summary>
        /// Gets singleton instance
        /// </summary>
        public static AzureB2CService Instance { get; } = new AzureB2CService();

        /// <summary>
        /// Get user
        /// </summary>
        /// <param name=")">access )</param>
        /// <returns><see cref="ADUser" /> class</returns>
        public async Task<ADUser> GetUserById(string id)
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                return await azureGraphRestApi.GetUserById(Configurations.AzureB2C.TenantId, id, BaseFunction.GetBearerAuthorization(masterToken));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Check if user is in group
        /// </summary>
        /// <param name="groupId">group id</param>
        /// <param name="userId">user id</param>
        /// <returns>True if user in group</returns>
        public async Task<bool> IsMemberOfGroup(string groupId, string userId)
        {
            var param = new IsMemberOfParam
            {
                GroupId = groupId,
                MemeberId = userId
            };

            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                var result = await azureGraphRestApi.IsMemberOf(Configurations.AzureB2C.TenantId, BaseFunction.GetBearerAuthorization(masterToken), param);
                return result?.Value ?? false;
            }
            catch (ApiException)
            {
                return false;
            }
        }

        /// <summary>
        /// Create an AD User
        /// </summary>
        /// <param name="parameters">user creation parameters</param>
        /// <returns>AD User</returns>
        public async Task<ADUser> CreateADUser(CreateADUserParameters parameters)
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                return await azureGraphRestApi.CreateUser(Configurations.AzureB2C.TenantId, BaseFunction.GetBearerAuthorization(masterToken), parameters);
            }
            catch (ApiException ex)
            {
                // success
                if (ex.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    return await ex.GetContentAsAsync<ADUser>();
                }

                Logger.Log?.LogError(ex.Message);
                return null;
            }
        }

        public async Task UpdateADUser(string userId, Dictionary<string, string> param)
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                await azureGraphRestApi.UpdateUser(Configurations.AzureB2C.TenantId, userId, BaseFunction.GetBearerAuthorization(masterToken), param);
            }
            catch (ApiException ex)
            {
                Logger.Log?.LogError(ex.Message);
            }
        }

        /// <summary>
        /// Add an user into group
        /// </summary>
        /// <param name="groupId">group id</param>
        /// <param name="userId">user id</param>
        /// <returns>true if success</returns>
        public async Task<bool> AddUserToGroup(string groupId, string userId)
        {
            var url = $"{Configurations.AzureB2C.GraphResource}/{Configurations.AzureB2C.TenantId}/directoryObjects/{userId}";
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                await azureGraphRestApi.AddUserToGroup(Configurations.AzureB2C.TenantId, groupId, BaseFunction.GetBearerAuthorization(masterToken), new AddUserToGroupParameter { Url = url });
                return true;
            }
            catch (ApiException ex)
            {
                // succes, but no content
                if (ex.StatusCode == System.Net.HttpStatusCode.NoContent) 
                {
                    return true;
                }

                Logger.Log?.LogError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Get an AD User
        /// </summary>
        /// <param name="email">user email</param>
        /// <returns>AD User</returns>
        public async Task<ADUser> GetADUserByEmail(string email)
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                var query = $"signInNames/any(x:x/value eq '{email.ToLower()}')";
                var results = await azureGraphRestApi.SearchUser(Configurations.AzureB2C.TenantId, BaseFunction.GetBearerAuthorization(masterToken), query);
                if (results.Values?.Count > 0)
                {
                    return results.Values[0];
                }
            }
            catch (ApiException ex)
            {
                Logger.Log?.LogError(ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Get all groups of user
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>list of group id</returns>
        public async Task<List<string>> GetUserGroups(string userId)
        {
            var param = new Dictionary<string, object>
            {
                { "securityEnabledOnly", true }
            };

            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                var groups = await azureGraphRestApi.GetUserGroups(Configurations.AzureB2C.TenantId, userId, BaseFunction.GetBearerAuthorization(masterToken), param);
                return groups?.GroupIds;
            }
            catch (ApiException)
            {
                return null;
            }
        }

        /// <summary>
        /// Remove user from a group
        /// </summary>
        /// <param name="groupId">group id</param>
        /// <param name="userId">user id</param>
        /// <returns>true if success</returns>
        public async Task<bool> RemoveUserFromGroup(string groupId, string userId)
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                var groups = await azureGraphRestApi.RemoveUserFromGroup(Configurations.AzureB2C.TenantId, userId, groupId, BaseFunction.GetBearerAuthorization(masterToken));
                return true;
            }
            catch (ApiException ex)
            {
                // success
                if (ex.StatusCode == System.Net.HttpStatusCode.NoContent) 
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Get access token in B2C policy
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="password">user password</param>
        /// <returns>ADToken class</returns>
        public Task<ADToken> GetB2CAccessToken(string email, string password)
        {
            var param = new Dictionary<string, object>
            {
                { "p", Configurations.AzureB2C.AuthPolicy },
                { "username", email.ToLower() },
                { "password", password },
                { "grant_type", Configurations.AzureB2C.GrantTypePassword },
                { "scope", $"openid {Configurations.AzureB2C.B2CClientId} offline_access" },
                { "client_id", Configurations.AzureB2C.B2CClientId },
                { "response_type", Configurations.AzureB2C.TokenType }
            };

            return b2cRestApi.GetAccessToken(param);
        }

        /// <summary>
        /// Refresh access token in B2C policy
        /// </summary>
        /// <param name="refreshToken">refresh token</param>
        /// <returns>ADToken class</returns>
        public Task<ADToken> RefreshB2CToken(string refreshToken)
        {
            var param = new Dictionary<string, object>
            {
                { "p", Configurations.AzureB2C.AuthPolicy },
                { "refresh_token", refreshToken },
                { "resource", Configurations.AzureB2C.B2CClientId },
                { "grant_type", Configurations.AzureB2C.GrantTypeRefreshToken },
                { "client_id", Configurations.AzureB2C.B2CClientId },
                { "response_type", Configurations.AzureB2C.TokenType }
            };

            return b2cRestApi.RefreshToken(param);
        }

        /// <summary>
        /// Get all groups of tenant
        /// </summary>
        /// <returns>Groups response</returns>
        public async Task<GroupsResponse> GetAllGroups()
        {
            try
            {
                var masterToken = await ADAccess.Instance.GetMasterKey();
                var groups = await azureGraphRestApi.GetAllGroups(Configurations.AzureB2C.TenantId, BaseFunction.GetBearerAuthorization(masterToken));
                return groups;
            }
            catch (ApiException ex)
            {
                Logger.Log?.LogError($"can not get groups {ex.Message}");
                return null;
            }
        }
    }
}
