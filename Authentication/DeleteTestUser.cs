using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Authentication.Shared.Library;
using Authentication.Shared;
using Authentication.Shared.Models;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;
using Dasync.Collections;

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
            string email = req.Query["email"];
            // only delete user from special domain
            if(!email.EndsWith(Configurations.Configuration["EmailTestDomain"]))
            {
                return CreateErrorResponse($"email {email} is invalid");
            }

            var adUser = await ADUser.FindByEmail(email);
            if(adUser == null)
            {
                return CreateErrorResponse($"Can not find user {email}");
            }

            var userId = adUser.ObjectId;
            log.LogInformation($"Delete user {userId}, {email}");

            await adUser.Delete();

            // delete cosmos user
            var dataService = new DataService();
            await dataService.DeleteById("User", userId, userId, ignoreNotFound: true);

            // delete all profiles of this user
            var query = new QueryDefinition("select * from c where c.partition = @id").WithParameter("@id", userId);
            var profiles = await dataService.QueryDocuments("Profile", query);

            await profiles.ParallelForEachAsync(
                async profile =>
                {
                    string profileId = profile.Value<string>("id");
                    await dataService.DeleteById("Profile", profileId, userId, ignoreNotFound: true);
                }, maxDegreeOfParallelism: 64
             );

            // delete all progress of this user
            query = new QueryDefinition("select * from c where c.partition = @id").WithParameter("@id", userId);
            var progresses = await dataService.QueryDocuments("Progress", query);

            await progresses.ParallelForEachAsync(
                async progress =>
                {
                    string progressId = progress.Value<string>("id");
                    await dataService.DeleteById("Progress", progressId, userId, ignoreNotFound: true);
                }, maxDegreeOfParallelism: 64
             );

            return CreateSuccessResponse();
        }
    }
}
