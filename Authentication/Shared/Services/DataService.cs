
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Generate cosmos data service.
    /// Only work with json data
    /// </summary>
    public interface IDataService
    {
        public Task<JObject> FindById(string table, string id);
        public Task SaveDocument(string table, JObject doc);
        public Task DeleteById(string table, string id, string partition, bool ignoreNotFound = false);
        public Task<List<JObject>> QueryDocuments(string table, QueryDefinition query, QueryRequestOptions options = null);
        public Task<List<JObject>> GetAll(string table);
    }

    public class DataService: IDataService
    {
        private readonly CosmosClient client;
        public DataService()
        {
            client = new CosmosClient(Configurations.Cosmos.DatabaseUrl, Configurations.Cosmos.DatabaseMasterKey);
        }

        public async Task<List<JObject>> GetAll(string table)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            var items = await LoadDocument(container, $"select * from c");
            return items;
        }

        public async Task<JObject> FindById(string table, string id)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var items = await LoadDocument(container, query, null);
            return items.Count == 0 ? null : items[0];
        }

        public async Task<List<JObject>> QueryDocuments(string table, QueryDefinition query, QueryRequestOptions options = null)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            var items = await LoadDocument(container, query, options);
            return items;
        }

        public async Task SaveDocument(string table, JObject doc)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            await container.UpsertItemAsync(doc);
        }

        public async Task DeleteById(string table, string id, string partition, bool ignoreNotFound = false)
        {
            if (string.IsNullOrWhiteSpace(partition))
            {
                partition = Configurations.Cosmos.DefaultPartition;
            }

            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);

            try
            {
                await container.DeleteItemAsync<object>(
               partitionKey: new PartitionKey(partition), requestOptions: new ItemRequestOptions(),
               id: id);
            }
            catch (CosmosException ex)
            {
                if(!(ex.StatusCode == System.Net.HttpStatusCode.NotFound && ignoreNotFound))
                {
                    throw ex;
                }
            }           
        }

        /// <summary>
        /// Load document from cosmos
        /// </summary>
        /// <param name="container">Container Object</param>
        /// <param name="query">Query</param>
        /// <param name="continuationToken">Continuation Token</param>
        /// <returns></returns>
        private async Task<List<JObject>> LoadDocument(Container container, string query, QueryRequestOptions options = null)
        {
            QueryDefinition queryDefinition = new QueryDefinition(query);
            FeedIterator<JObject> queryResultSetIterator = container.GetItemQueryIterator<JObject>(queryDefinition, null, options);
            List<JObject> documents = new List<JObject>();
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<JObject> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                documents.AddRange(currentResultSet);
            }
            return documents;
        }

        /// <summary>
        /// Load document from cosmos
        /// </summary>
        /// <param name="container">Container Object</param>
        /// <param name="query">Query</param>
        /// <param name="continuationToken">Continuation Token</param>
        /// <returns></returns>
        private async Task<List<JObject>> LoadDocument(Container container, QueryDefinition queryDefinition, QueryRequestOptions options)
        {
            FeedIterator<JObject> queryResultSetIterator = container.GetItemQueryIterator<JObject>(queryDefinition, null, options);
            List<JObject> documents = new List<JObject>();
            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<JObject> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                documents.AddRange(currentResultSet);
            }
            return documents;
        }
    }
}
