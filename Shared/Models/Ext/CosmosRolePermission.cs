using System.Collections.Generic;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Extensions;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos role permission
    /// This class has methods to manage cosmos user and permissions
    /// </summary>
    public partial class CosmosRolePermission
    {
        public bool IsReadOnly
        {
            get { return Permission.EqualsIgnoreCase("read") || Permission.EqualsIgnoreCase("id-read"); }
        }

        /// <summary>
        /// Create cosmos user
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>User class</returns>
        public static Task<User> CreateCosmosUser(string userId)
        {
            return DataService.Instance.CreateUser(userId);
        }

        /// <summary>
        /// Query cosmos permissions by table name
        /// </summary>
        /// <param name="table">table name</param>
        /// <returns>List of CosmosRolePermission</returns>
        public static Task<List<CosmosRolePermission>> QueryByTable(string table)
        {
            var query = new QueryDefinition("select * from c where c.table = @table").WithParameter("@table", table);
            return DataService.Instance.QueryDocuments<CosmosRolePermission>("RolePermissions", query);
        }

        /// <summary>
        /// Query cosmos permissions by role
        /// </summary>
        /// <param name="role">role name</param>
        /// <returns>List of CosmosRolePermission</returns>
        public static Task<List<CosmosRolePermission>> QueryByRole(string role)
        {
            var query = new QueryDefinition("select * from c where c.role = @role").WithParameter("@role", role);
            return DataService.Instance.QueryDocuments<CosmosRolePermission>("RolePermissions", query);
        }

        public static Task<List<CosmosRolePermission>> QueryByIdPermissions()
        {
            var query = new QueryDefinition("select * from c where c.permission = @p1 or c.permission = @p2")
                .WithParameter("@p1", "id-read").WithParameter("@p2", "id-read-write");
            return DataService.Instance.QueryDocuments<CosmosRolePermission>("RolePermissions", query);
        }

        /// <summary>
        /// Get all the defined tables in database
        /// </summary>
        /// <returns>List of table names</returns>
        public static Task<List<string>> GetAllTables()
        {
            return DataService.Instance.GetAllTables();
        }

        /// <summary>
        /// Create cosmos permission
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionId">permission id</param>
        /// /// <param name="partition">partition key</param>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreateCosmosPermission(string userId, string permissionId, string partition = null)
        {
            return DataService.Instance.CreatePermission(userId, permissionId, IsReadOnly, Table, partition);
        }
    }
}
