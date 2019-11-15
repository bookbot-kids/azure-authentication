using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

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
                return (List<ADGroup>) cacheItem.Value;
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
            if (string.IsNullOrWhiteSpace(Name))
            {
                return new List<PermissionProperties>();
            }

            var cache = MemoryCache.Default;
            var cacheKey = $"role_{Name}";
            if (cache.GetCacheItem(cacheKey) != null)
            {
                return (List<PermissionProperties>)cache.GetCacheItem(cacheKey).Value;
            }
            else
            {
                // cache is expired, get new one
                var permissions = await DataService.Instance.GetCosmosPermissions(Name);
                cache.Set(cacheKey, permissions, new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(20)
                });

                return permissions;
            }
        }
    }
}
