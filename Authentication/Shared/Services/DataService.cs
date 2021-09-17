
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
    }

    public class DataService: IDataService
    {
        private readonly CosmosClient client;
        public DataService()
        {
            client = new CosmosClient(Configurations.Cosmos.DatabaseUrl, Configurations.Cosmos.DatabaseMasterKey);
        }

        public async Task<JObject> FindById(string table, string id)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            var items = await LoadDocument(container, $"select * from c where c.id = {id}");
            return items.Count == 0 ? null : items[0];
        }

        public async Task SaveDocument(string table, JObject doc)
        {
            var container = client.GetDatabase(Configurations.Cosmos.DatabaseId).GetContainer(table);
            await container.UpsertItemAsync(doc);
        }

        /// <summary>
        /// Load document from cosmos
        /// </summary>
        /// <param name="container">Container Object</param>
        /// <param name="query">Query</param>
        /// <param name="continuationToken">Continuation Token</param>
        /// <returns></returns>
        private async Task<List<JObject>> LoadDocument(Container container, string query)
        {
            QueryDefinition queryDefinition = new QueryDefinition(query);
            FeedIterator<JObject> queryResultSetIterator = container.GetItemQueryIterator<JObject>(queryDefinition);
            List<JObject> documents = new List<JObject>();
            if (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<JObject> currentResultSet = await queryResultSetIterator.ReadNextAsync();
                documents.AddRange(currentResultSet);
            }
            return documents;
        }
    }
}
