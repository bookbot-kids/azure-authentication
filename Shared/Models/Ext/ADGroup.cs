using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Utils;
using Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD Group
    /// This class contains all the methods to manage AD group
    /// </summary>
    public sealed partial class ADGroup
    {
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
            var result = new List<PermissionProperties>();
            if (string.IsNullOrWhiteSpace(Name))
            {
                return result;
            }

            // create user if needed
            await DataService.Instance.CreateUser(Name);

            // admin should have read-write permission for all except tables that have id-read or id-read-write
            if (Name.EqualsIgnoreCase("admin"))
            {
                List<Task<PermissionProperties>> adminTasks = new List<Task<PermissionProperties>>();
                var tables = await CosmosRolePermission.GetAllTables();
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
            var rolePermissions = await CosmosRolePermission.QueryByRole(Name);
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

        private async Task<PermissionProperties> GetOrCreateAdminPermission(string table)
        {
            var permission = await DataService.Instance.GetPermission("admin", table);
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await DataService.Instance.CreatePermission("admin", table, false, table);
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

        private async Task<PermissionProperties> GetOrCreatePermission(CosmosRolePermission rolePermission)
        {
            // get cosmos permission by id: role_name/table_name
            var permission = await DataService.Instance.GetPermission(Name, rolePermission.Table);
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await rolePermission.CreateCosmosPermission(Name, rolePermission.Table);
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
                    // rolePermission is changed, need to update in cosmos
                    var updatedPermission = await DataService.Instance.ReplacePermission(Name, rolePermission.Table,
                        rolePermission.Permission.EqualsIgnoreCase("read"), rolePermission.Table);
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
    }
}
