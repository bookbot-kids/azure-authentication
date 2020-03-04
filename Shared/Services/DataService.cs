using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Cosmos database service
    /// A singleton class that manages cosmos database
    /// </summary>
    public class DataService
    {
        /// <summary>
        /// Cosmos document client
        /// </summary>
        private readonly CosmosClient client;

        /// <summary>
        /// Prevents a default instance of the <see cref="DataService"/> class from being created
        /// </summary>
        private DataService()
        {
            client = new CosmosClient(Configurations.Cosmos.DatabaseUrl, Configurations.Cosmos.DatabaseMasterKey);
        }

        /// <summary>
        /// Gets singleton instance
        /// </summary>
        public static DataService Instance { get; } = new DataService();

        /// <summary>
        /// Query documents from a collection
        /// </summary>
        /// <typeparam name="T">Document type</typeparam>
        /// <param name="collectionName">collection name</param>
        /// <param name="query">query paramter</param>
        /// <param name="partition">partition key</param>
        /// <returns>List of documents</returns>
        public async Task<List<T>> QueryDocuments<T>(string collectionName, QueryDefinition query, string partition = null)
        {
            var collection = client.GetContainer(Configurations.Cosmos.DatabaseId, collectionName);
            var partitionKey = new PartitionKey(partition ?? Configurations.Cosmos.DefaultPartition);
            var queryOption = new QueryRequestOptions { PartitionKey = partitionKey };
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
            try
            {
                var permission = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetUser(userId).GetPermission(permissionName);
                return await permission.ReadAsync();
            }
            catch (CosmosException)
            {
            }

            return null;
        }

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
}
