using System;
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
        /// <param name="userId">user id</param>
        /// <returns>List of Connection</returns>
        public static Task<List<Connection>> QueryByShareUser(string userId)
        {
            var query = new QueryDefinition("select * from c where c.user2 = @userId").WithParameter("@userId", userId);
            return DataService.Instance.QueryDocuments<Connection>("Connection", query, partition: userId);
        }

        /// <summary>
        /// Query connection by user 1 & user 2
        /// </summary>
        /// <param name="user1"> user 1</param>
        /// <param name="user2">user 2</param>
        /// <returns>Connection class</returns>
        public static async Task<Connection> QueryBy2Users(string user1, string user2)
        {
            var query = new QueryDefinition("select * from c where c.user1 = @user1 and c.user2 = @user2")
                .WithParameter("@user1", user1).WithParameter("@user2", user2);
            var result = await DataService.Instance.QueryDocuments<Connection>("Connection", query, partition: user2);
            return result.Count > 0? result[0] : null;
        }

        /// <summary>
        /// Create or update Connection
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<Connection> CreateOrUpdate()
        {
            if(Id == null)
            {
                Id = Guid.NewGuid().ToString();
            }

            if (Partition == null)
            {
                Partition = User2;
            }

            if(CreatedAt == default)
            {
                CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            }

            UpdatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            return DataService.Instance.CreateOrUpdateDocument("Connection", Id, this, Partition);
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
        public Task<PermissionProperties> GetProfilePermission()
        {
            return DataService.Instance.GetPermission(User2, "Profile-shared");
        }

        /// <summary>
        /// Create profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreateProfilePermission()
        {
            return DataService.Instance.CreatePermission(User2, "Profile-shared", IsReadOnly, "Profile", User1);
        }

        /// <summary>
        /// Update profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> UpdateProfilePermission()
        {
            return DataService.Instance.ReplacePermission(User2, "Profile-shared", IsReadOnly, "Profile", User1);
        }

    }
}
