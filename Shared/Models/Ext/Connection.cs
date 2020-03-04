using System.Collections.Generic;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    public partial class Connection
    {
        /// <summary>
        /// Query connection by shared user id
        /// </summary>
        /// <param name="table">user id</param>
        /// <returns>List of CosmosRolePermission</returns>
        public static Task<List<Connection>> QueryByShareUser(string userId)
        {
            var query = new QueryDefinition("select * from c where c.user2 = @userId").WithParameter("@userId", userId);
            return DataService.Instance.QueryDocuments<Connection>("Connection", query);
        }

        /// <summary>
        /// Get cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> GetPermission()
        {
            return DataService.Instance.GetPermission(User2, Table + "-shared");
        }

        /// <summary>
        /// Create cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreatePermission()
        {
            return DataService.Instance.CreatePermission(User2, Table + "-shared", IsReadOnly, Table, User1);
        }

        /// <summary>
        /// Update cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> UpdatePermission()
        {
            return DataService.Instance.ReplacePermission(User2, Table + "-shared", IsReadOnly, Table, User1);
        }

        /// <summary>
        /// Get profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> GetProfilePermission(string profileId)
        {
            return DataService.Instance.GetPermission(User2, "Profile-shared");
        }

        /// <summary>
        /// Create profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreateProfilePermission(string profileId)
        {
            return DataService.Instance.CreatePermission(User2, "Profile-shared", IsReadOnly, "Profile", profileId);
        }

        /// <summary>
        /// Update profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> UpdateProfilePermission(string profileId)
        {
            return DataService.Instance.ReplacePermission(User2, "Profile-shared", IsReadOnly, "Profile", profileId);
        }

    }
}
