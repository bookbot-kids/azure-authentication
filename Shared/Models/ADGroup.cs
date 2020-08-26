using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD Group See <see href="https://docs.microsoft.com/en-us/previous-versions/azure/ad/graph/api/groups-operations">document</see> <br/>
    /// This class contains all the properties groups in b2c and support to manage (CRUD) all the groups
    /// </summary>
    public class ADGroup
    {
        #region Properties
        /// <summary>
        /// Gets or sets object type
        /// </summary>
        [JsonProperty(PropertyName = "odata.type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets id of group
        /// </summary>
        [JsonProperty(PropertyName = "objectId")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets name of group
        /// </summary>
        [JsonProperty(PropertyName = "displayName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description of group
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        #endregion

        #region Methods
        /// <summary>
        /// Get ADGroup instance from its name
        /// </summary>
        /// <param name="name">group name</param>
        /// <returns>ADGroup instance</returns>
        public static async Task<ADGroup> FindByName(string name)
        {
            var groups = await GetAllGroups();
            if (name == null || groups == null)
            {
                return null;
            }

            name = name.ToLower();
            foreach (var group in groups)
            {
                if (name.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Get ADGroup instance from its id
        /// </summary>
        /// <param name="id">group id</param>
        /// <returns>ADGroup instance</returns>
        public static async Task<ADGroup> FindById(string id)
        {
            var groups = await GetAllGroups();
            if (id == null || groups == null)
            {
                return null;
            }

            foreach (var group in groups)
            {
                if (id == group.Id)
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Get all b2c groups<br/>
        /// If the group list is in the cache, then return it. Otherwise refresh cache and return
        /// </summary>
        /// <returns>List of groups or null</returns>
        public static async Task<List<ADGroup>> GetAllGroups()
        {
            // get groups from memory cache
            var cache = MemoryCache.Default;
            var cacheItem = cache.GetCacheItem("groups");
            if (cacheItem != null)
            {
                return (List<ADGroup>)cacheItem.Value;
            }

            // get latest list if there is no cache value
            var result = await AzureB2CService.Instance.GetAllGroups();
            if (result?.Groups != null)
            {
                // cache in 20 minutes
                var policy = new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20)
                };

                cache.Set("groups", result.Groups, policy);
                return result.Groups;
            }

            return null;
        }

        /// <summary>
        /// Add user into group
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>True if success</returns>
        public Task<bool> AddUser(string userId)
        {
            return AzureB2CService.Instance.AddUserToGroup(Id, userId);
        }

        /// <summary>
        /// Remove user from a group
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>true if success</returns>
        public Task<bool> RemoveUser(string userId)
        {
            return AzureB2CService.Instance.RemoveUserFromGroup(Id, userId);
        }

        /// <summary>
        /// Check if group has a user
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>True if success</returns>
        public Task<bool> HasUser(string userId)
        {
            return AzureB2CService.Instance.IsMemberOfGroup(Id, userId);
        }

        /// <summary>
        /// Get cosmos permissions of group
        /// If the permission list is in the cache, then return it. Otherwise refresh cache and return
        /// </summary>
        /// <returns>List of permissions</returns>
        public async Task<List<PermissionProperties>> GetPermissions()
        {
            var tracking = new TimeTracking();
            var result = new List<PermissionProperties>();
            if (string.IsNullOrWhiteSpace(Name))
            {
                return result;
            }

            // create user if needed
            tracking.BeginTracking();
            await CosmosService.Instance.CreateUser(Name);
            tracking.EndTracking($"Create user {Name}");

            // admin should have read-write permission for all except tables that have id-read or id-read-write
            if (Name.EqualsIgnoreCase("admin"))
            {
                List<Task<PermissionProperties>> adminTasks = new List<Task<PermissionProperties>>();
                tracking.BeginTracking();
                var tables = await CosmosRolePermission.GetAllTables();
                tracking.EndTracking($"Query all RolePermissions records for admin");
                tracking.BeginTracking();
                foreach (var table in tables)
                {
                    var rolePermisisons = await CosmosRolePermission.QueryByTable(table);
                    if (rolePermisisons != null)
                    {
                        var containsIdPermission = rolePermisisons.Exists(e => e.Permission.EqualsIgnoreCase("id-read")
                        || e.Permission.EqualsIgnoreCase("id-read-write"));
                        if (containsIdPermission)
                        {
                            continue;
                        }
                    }

                    adminTasks.Add(GetOrCreateAdminPermission(table));
                }

                await Task.WhenAll(adminTasks);
                tracking.EndTracking($"Executed admin group permissions tasks");
                foreach (var task in adminTasks)
                {
                    if (task.Result != null)
                    {
                        var data = task.Result;
                        result.Add(data);
                    }

                }

                return result;
            }


            List<Task<PermissionProperties>> tasks = new List<Task<PermissionProperties>>();
            tracking.BeginTracking();
            var rolePermissions = await CosmosRolePermission.QueryByRole(Name);
            tracking.EndTracking($"Query all RolePermissions records for {Name}");
            tracking.BeginTracking();
            foreach (var rolePermission in rolePermissions)
            {
                // don't return none, id-read, id-read-write permission in that group
                if (rolePermission.Permission.EqualsIgnoreCase("none")
                    || rolePermission.Permission.EqualsIgnoreCase("id-read")
                    || rolePermission.Permission.EqualsIgnoreCase("id-read-write"))
                {
                    continue;
                }

                tasks.Add(GetOrCreatePermission(rolePermission));
            }


            await Task.WhenAll(tasks);
            tracking.EndTracking($"All group permissions tasks");

            foreach (var task in tasks)
            {
                if (task.Result != null)
                {
                    var data = task.Result;
                    result.Add(data);
                }

            }

            return result;
        }

        /// <summary>
        /// Get or create Cosmos permission (resource tokens) for admin role.
        /// It will create Cosmos permision if does not exist
        /// </summary>
        /// <param name="table">The cosmos table</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateAdminPermission(string table)
        {
            var tracking = new TimeTracking();
            tracking.BeginTracking();
            var permission = await CosmosService.Instance.GetPermission("admin", table);
            tracking.EndTracking($"GetAddPermission for {table}");
            if (permission == null)
            {
                // create permission if not exist
                tracking.BeginTracking();
                var newPermission = await CosmosService.Instance.CreatePermission("admin", table, false, table);
                tracking.EndTracking($"Create new permission for {table}");
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogWarning($"error create permission admin - ${table}");
                }
            }
            else
            {
                return permission;
            }

            return null;
        }

        /// <summary>
        /// Get or create Cosmos permission (resource tokens) base on role (AD group)
        /// It will create Cosmos permision if does not exist
        /// </summary>
        /// <param name="rolePermission">The role permission record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreatePermission(CosmosRolePermission rolePermission)
        {
            var tracking = new TimeTracking();
            // get cosmos permission by id: role_name/table_name
            tracking.BeginTracking();
            var permission = await CosmosService.Instance.GetPermission(Name, rolePermission.Table);
            tracking.EndTracking($"GetPermission {rolePermission.Table} for {Name}");
            if (permission == null)
            {
                // create permission if not exist
                tracking.BeginTracking();
                var newPermission = await rolePermission.CreateCosmosPermission(Name, rolePermission.Table);
                tracking.EndTracking($"Create new permission {rolePermission.Table} for {Name}");
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogWarning($"error create permission ${Name} ${rolePermission.Table}");
                }

            }
            else
            {
                if ((rolePermission.Permission.EqualsIgnoreCase("read") && permission.PermissionMode != PermissionMode.Read)
                    || (rolePermission.Permission.EqualsIgnoreCase("read-write") && permission.PermissionMode != PermissionMode.All))
                {
                    tracking.BeginTracking();
                    // rolePermission is changed, need to update in cosmos
                    var updatedPermission = await CosmosService.Instance.ReplacePermission(Name, rolePermission.Table,
                        rolePermission.Permission.EqualsIgnoreCase("read"), rolePermission.Table);
                    tracking.EndTracking($"Update permission {rolePermission.Table} for {Name}");
                    if (updatedPermission != null)
                    {
                        return updatedPermission;
                    }
                    else
                    {
                        Logger.Log?.LogWarning($"error update permission ${Name} ${rolePermission.Table}");
                    }
                }
                else
                {
                    return permission;
                }
            }

            return null;
        }
        #endregion
    }
}
