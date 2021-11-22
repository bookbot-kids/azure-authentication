using System;
using System.Net.Http;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Refit;

namespace Authentication.Shared.Services
{
    public interface IAzureMSGraphService
    {
        public Task DeleteADUser(string userId);
    }

    public class AzureMSGraphService : IAzureMSGraphService
    {
        public interface MSGraphAPI
        {
            /// <summary>
            /// Remove user
            /// </summary>
            /// <param name="tenantId">Tenant id</param>
            /// <param name="userId">User id</param>
            /// <param name="accessToken">access token</param>
            /// <returns>Api result</returns>
            [Headers("Accept: application/json")]
            [Delete("/users/{userId}")]
            Task<APIResult> DeleteUser([AliasAs("userId")] string userId, [Header("Authorization")] string accessToken);
        }

        public static AzureMSGraphService Instance { get; } = new AzureMSGraphService();

        private MSGraphAPI graphAPI;

        public AzureMSGraphService()
        {
            var client = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.AzureB2C.GraphApiUrl) };
            graphAPI = RestService.For<MSGraphAPI>(client);
        }

        public async Task DeleteADUser(string userId)
        {
            var token = await MicrosoftService.Instance.GetMasterToken(isV2: true);
            await graphAPI.DeleteUser(userId, BaseFunction.GetBearerAuthorization(token.AccessToken));
        }
    }
}
