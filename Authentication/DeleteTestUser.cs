using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Library;
using Authentication.Shared;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;
using Dasync.Collections;
using Extensions;
using Newtonsoft.Json.Linq;
using System;

namespace Authentication
{
    public class DeleteTestUser: BaseFunction
    {
        [FunctionName("DeleteTestUser")]
        public  async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            for(var i = 77; i <= 130; i++)
            {
                var email = $"duc+{i}@bookbotkids.com";
                log.LogInformation($"Start to delete user {email}");
                var user = await AWSService.Instance.FindUserByEmail(email);
                if (user == null)
                {
                    log.LogError($"email {email} is invalid");
                    continue;
                }

                var userId = AWSService.Instance.GetUserAttributeValue(user, "preferred_username");
                log.LogInformation($"Delete user {user.Username}, cosmos id {userId}, {email}");

                var dataService = new DataService();
               

                // delete cosmos user
                await DeleteUserRecords(dataService, "User", null,
                    new QueryDefinition("select * from c where c.email = @email").WithParameter("@email", email));

                // delete all data of this user
                foreach (var table in Configurations.Cosmos.UserTablesToClear)
                {
                    await DeleteUserRecords(dataService, table, userId);
                    log.LogInformation($"Deleted user {email} table {table}");
                }

                // delete cognito user
                await AWSService.Instance.DeleteUser(user.Username);
                log.LogInformation($"Deleted user {email}");
            }
            

            return CreateSuccessResponse();
        }

        private async Task DeleteUserRecords(DataService dataService, string table, string userId, QueryDefinition query = null)
        {
            if (query == null)
            {
                query = new QueryDefinition("select * from c where c.partition = @id").WithParameter("@id", userId);
            }

            var documents = await dataService.QueryDocuments(table, query);
            Console.WriteLine($"Delete {documents.Count} in table {table}");
            await documents.ParallelForEachAsync(
                async doc =>
                {
                    var id = doc.GetValue("id").Value<string>();
                    await dataService.DeleteById(table, id, userId, ignoreNotFound: true);
                    Console.WriteLine($"Deleted record {id} in table {table}");
                }, maxDegreeOfParallelism: 64
             );
        }
    }
}
