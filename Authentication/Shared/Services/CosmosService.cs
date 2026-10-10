using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Cosmos database service
    /// A singleton class that manages cosmos database
    /// </summary>
    public class CosmosService
    {
        /// <summary>
        /// Cosmos document client
        /// </summary>
        private readonly CosmosClient client;

        /// <summary>
        /// Prevents a default instance of the <see cref="CosmosService"/> class from being created
        /// </summary>
        private CosmosService()
        {
            client = new CosmosClient(Configurations.Cosmos.DatabaseUrl, Configurations.Cosmos.DatabaseMasterKey);
            ensuredUsers = new OncePerKey("cosmos-users", CreateUserIfMissing, TimeSpan.FromMinutes(30));
        }

        /// <summary>
        /// Cosmos users this instance has already made sure exist
        /// </summary>
        private readonly OncePerKey ensuredUsers;

        /// <summary>
        /// Gets singleton instance
        /// </summary>
        public static CosmosService Instance { get; } = new CosmosService();

        /// <summary>
        /// Query documents from a collection
        /// </summary>
        /// <typeparam name="T">Document type</typeparam>
        /// <param name="collectionName">collection name</param>
        /// <param name="query">query paramter</param>
        /// <param name="partition">partition key</param>
        /// <param name="crossPartition">query cross partition</param>
        /// <returns>List of documents</returns>
        public async Task<List<T>> QueryDocuments<T>(string collectionName, QueryDefinition query, string partition = null, bool crossPartition = false)
        {
            var collection = client.GetContainer(Configurations.Cosmos.DatabaseId, collectionName);
            var partitionKey = new PartitionKey(partition ?? Configurations.Cosmos.DefaultPartition);
            var queryOption = crossPartition ? new QueryRequestOptions() :
                new QueryRequestOptions { PartitionKey = partitionKey };
            var feeds = collection.GetItemQueryIterator<T>(query, requestOptions: queryOption);
            List<T> ret = new List<T>();
            while (feeds.HasMoreResults)
            {
                FeedResponse<T> currentResultSet = await feeds.ReadNextAsync();
                foreach (T family in currentResultSet)
                {
                    ret.Add(family);
                }
            }

            return ret;
        }

        public async Task<T> CreateOrUpdateDocument<T>(string collectionName, string id, T doc, string partition = null)
        {
            var collection = client.GetContainer(Configurations.Cosmos.DatabaseId, collectionName);
            var partitionKey = new PartitionKey(partition ?? Configurations.Cosmos.DefaultPartition);
            
            try
            {
                var newItem = await collection.CreateItemAsync(doc, partitionKey: partitionKey);
                return newItem.Resource;
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    var newItem = await collection.ReplaceItemAsync(doc, id, partitionKey: partitionKey);
                    return newItem.Resource;
                }
               
            } catch (Exception ex)
            {
                Logger.Log?.LogError("CreateOrUpdateDocument error " + ex.Message);
            }

            return default;
        }

        /// <summary>
        /// Make sure the cosmos user exists, creating it at most once per instance every 30 minutes.
        /// Creating users is a Cosmos metadata operation, which has a low account-wide rate limit,
        /// and every token request used to create its users again (409 when they already exist).
        /// </summary>
        /// <param name="userId">Cosmos user id</param>
        /// <returns>Async task</returns>
        public Task EnsureUser(string userId)
        {
            return ensuredUsers.Run(userId);
        }

        private async Task<bool> CreateUserIfMissing(string userId)
        {
            try
            {
                await client.GetDatabase(Configurations.Cosmos.DatabaseId).CreateUserAsync(userId);
                return true;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                return true;
            }
            catch (CosmosException ex)
            {
                // not remembered, so the next request tries again
                Logger.Log?.LogWarning($"Create cosmos user {userId} error {(int)ex.StatusCode}/{ex.SubStatusCode}");
                return false;
            }
        }

        /// <summary>
        /// Create cosmos user if not exist
        /// </summary>
        /// <param name="userId">Cosmos user id</param>
        /// <returns>Cosmos user</returns>
        public async Task<User> CreateUser(string userId)
        {
            try
            {
                var result = await client.GetDatabase(Configurations.Cosmos.DatabaseId).CreateUserAsync(userId);
                return result?.User;
            }
            catch (CosmosException)
            {
            }

            return null;
        }

        /// <summary>
        /// List all users
        /// </summary>
        /// <returns>List of users</returns>
        public async Task<List<UserProperties>> ListUsers()
        {
            var resultSet = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUserQueryIterator<UserProperties>();
            var list = new List<UserProperties>();
            while (resultSet.HasMoreResults)
            {
                FeedResponse<UserProperties> iterator = await resultSet.ReadNextAsync();
                foreach (var user in iterator)
                {
                    list.Add(user);
                }
            }

            return list;
        }

        /// <summary>
        /// Create cosmos permission if not exist
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionId">permission id</param>
        /// <param name="readOnly">is read only</param>
        /// <param name="tableName">table name</param>
        /// <param name="partition">partition key</param>
        /// <returns>Permission class</returns>
        public async Task<PermissionProperties> CreatePermission(string userId, string permissionId, bool readOnly, string tableName, string partition = null)
        {
            try
            {
                var collection = client.GetContainer(Configurations.Cosmos.DatabaseId, tableName);
                var permission = new PermissionProperties(
                    permissionId,
                    readOnly ? PermissionMode.Read : PermissionMode.All,
                    collection,
                    new PartitionKey(partition ?? Configurations.Cosmos.DefaultPartition));
                var result = await client.GetDatabase(Configurations.Cosmos.DatabaseId)
                    .GetUser(userId).CreatePermissionAsync(permission, tokenExpiryInSeconds: Configurations.Cosmos.ResourceTokenExpiration);
                return result.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                // a parallel request created it first: use that one
                return (await ReadPermission(userId, permissionId)).Permission;
            }
            catch (CosmosException)
            {
            }

            return null;
        }

        /// <summary>
        /// Clear all users and permissions
        /// </summary>
        /// <returns>Async task</returns>
        public async Task ClearAllAsync()
        {
            var users = await ListUsers();
            foreach (var user in users)
            {
                var resultSet = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(user.Id).GetPermissionQueryIterator<PermissionProperties>();
                while (resultSet.HasMoreResults)
                {
                    FeedResponse<PermissionProperties> iterator = await resultSet.ReadNextAsync();
                    foreach (var permission in iterator)
                    {
                        await client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(user.Id).GetPermission(permission.Id).DeleteAsync();
                    }
                }

                await client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(user.Id).DeleteAsync();
            }
        }

        /// <summary>
        /// Get cosmos permissions of user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>List of permissions</returns>
        public async Task<List<PermissionProperties>> GetPermissions(string userId)
        {
            var result = new List<PermissionProperties>();
            try
            {
                var resultSet = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(userId).GetPermissionQueryIterator<PermissionProperties>();
                while (resultSet.HasMoreResults)
                {
                    FeedResponse<PermissionProperties> iterator = await resultSet.ReadNextAsync();
                    foreach (var permission in iterator)
                    {
                        result.Add(permission);
                    }
                }
            }
            catch (CosmosException)
            {
            }

            return result;
        }

        /// <summary>
        /// Get cosmos permission
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionName">permission name</param>
        /// <returns>Permission object</returns>
        public async Task<PermissionProperties> GetPermission(string userId, string permissionName)
        {
            return (await ReadPermission(userId, permissionName)).Permission;
        }

        /// <summary>
        /// Read a cosmos permission and say why there is none.
        /// Missing is true only when Cosmos reports it does not exist (404), so callers create permissions
        /// only then. A throttled read (429) means nothing about existence, and creating on it just adds
        /// more load to the same rate limit.
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionName">permission name</param>
        /// <returns>The permission, or null and whether it is missing</returns>
        public async Task<(PermissionProperties Permission, bool Missing)> ReadPermission(string userId, string permissionName)
        {
            try
            {
                var permission = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(userId).GetPermission(permissionName);
                PermissionProperties result = await permission.ReadAsync(tokenExpiryInSeconds: Configurations.Cosmos.ResourceTokenExpiration);
                return (result, false);
            }
            catch (CosmosException ex)
            {
                var missing = IsMissing(ex);
                if (!missing)
                {
                    Logger.Log?.LogError("GetPermission error " + ex.Message);
                }

                return (null, missing);
            }
            catch (NullReferenceException ex)
            {
                Logger.Log?.LogError("GetPermission null error " + ex.Message);
            }

            return (null, false);
        }

        /// <summary>
        /// Whether a failed cosmos read means the resource does not exist
        /// </summary>
        internal static bool IsMissing(CosmosException ex) => ex.StatusCode == HttpStatusCode.NotFound;

        /// <summary>
        /// Remove permission 
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionName">permission name</param>
        /// <returns>Permission propert</returns>
        public async Task<PermissionProperties> RemovePermission(string userId, string permissionName)
        {
            try
            {
                var permission = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(userId).GetPermission(permissionName);
                return await permission.DeleteAsync();
            }
            catch (CosmosException)
            {
            }

            return null;
        }

        /// <summary>
        /// Replace permission by a new one
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="permissionId">permission id</param>
        /// <param name="readOnly">is read only</param>
        /// <param name="tableName">table name</param>
        /// <param name="partition">partition key</param>
        /// <returns>Permission propert</returns>
        public async Task<PermissionProperties> ReplacePermission(string userId, string permissionId, bool readOnly, string tableName, string partition = null)
        {
            try
            {
                var collection = client.GetContainer(Configurations.Cosmos.DatabaseId, tableName);
                var permission = new PermissionProperties(
                    permissionId,
                    readOnly ? PermissionMode.Read : PermissionMode.All,
                    collection,
                    new PartitionKey(partition ?? Configurations.Cosmos.DefaultPartition));
                var result = await client.GetDatabase(Configurations.Cosmos.DatabaseId)
                    .GetUser(userId).UpsertPermissionAsync(permission, tokenExpiryInSeconds: Configurations.Cosmos.ResourceTokenExpiration);
                return result?.Resource;
            }
            catch (CosmosException)
            {
            }

            return null;
        }

        /// <summary>
        /// Get all the defined tables in database
        /// </summary>
        /// <returns>List of table names</returns>
        public async Task<List<string>> GetAllTables()
        {
            var result = new List<string>();
            var db = client.GetDatabase(Configurations.Cosmos.DatabaseId);
            FeedIterator<ContainerProperties> resultSetIterator = db.GetContainerQueryIterator<ContainerProperties>();
            while (resultSetIterator.HasMoreResults)
            {
                foreach (ContainerProperties container in await resultSetIterator.ReadNextAsync())
                {
                    result.Add(container.Id);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Runs some work at most once per key: parallel callers share one attempt, and a successful
    /// attempt is remembered for a while. A failed attempt is not remembered, so the next call retries.
    /// </summary>
    internal sealed class OncePerKey
    {
        private readonly Func<string, Task<bool>> work;
        private readonly TimeSpan remember;
        private readonly MemoryCache done;
        private readonly ConcurrentDictionary<string, Lazy<Task>> running = new ConcurrentDictionary<string, Lazy<Task>>();

        /// <param name="name">cache name</param>
        /// <param name="work">the work; returns true when it succeeded</param>
        /// <param name="remember">how long a success is remembered</param>
        public OncePerKey(string name, Func<string, Task<bool>> work, TimeSpan remember)
        {
            this.work = work;
            this.remember = remember;
            done = new MemoryCache(name);
        }

        public Task Run(string key)
        {
            if (done.Contains(key))
            {
                return Task.CompletedTask;
            }

            return running.GetOrAdd(key, k => new Lazy<Task>(() => RunCore(k))).Value;
        }

        private async Task RunCore(string key)
        {
            // never complete synchronously, so the running entry is always stored before it is removed
            await Task.Yield();
            try
            {
                if (await work(key))
                {
                    done.Set(key, true, DateTimeOffset.UtcNow.Add(remember));
                }
            }
            finally
            {
                running.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// A value loaded on demand and kept for a time. Parallel callers share one load. When a refresh
    /// fails, the previous value keeps being served and the next refresh waits a minute, so an outage
    /// or throttling is not hit again by every request.
    /// </summary>
    internal sealed class CachedValue<T> where T : class
    {
        private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(1);
        private readonly Func<Task<T>> load;
        private readonly TimeSpan ttl;
        private readonly Func<DateTime> utcNow;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private T value;
        private DateTime loadedAt;

        public CachedValue(Func<Task<T>> load, TimeSpan ttl, Func<DateTime> utcNow = null)
        {
            this.load = load;
            this.ttl = ttl;
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<T> Get()
        {
            if (IsFresh())
            {
                return value;
            }

            await gate.WaitAsync();
            try
            {
                if (IsFresh())
                {
                    return value;
                }

                try
                {
                    value = await load();
                    loadedAt = utcNow();
                    return value;
                }
                catch (Exception ex) when (value != null)
                {
                    Logger.Log?.LogWarning($"Refresh failed ({ex.GetType().Name}), using the cached value");
                    loadedAt = utcNow() - ttl + RetryAfterFailure;
                    return value;
                }
            }
            finally
            {
                gate.Release();
            }
        }

        private bool IsFresh() => value != null && utcNow() - loadedAt < ttl;
    }
}
