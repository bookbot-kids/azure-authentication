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
using Extensions;

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
             // validate client token
            string clientToken = req.Query["client_token"];
            if(string.IsNullOrWhiteSpace(clientToken)) {
                return CreateErrorResponse("client_token is missing", StatusCodes.Status401Unauthorized);
            }
            var (validateResult, clientTokenMessage, payload) = TokenService.ValidateClientToken(clientToken, Configurations.JWTToken.TokenClientSecret,
                 Configurations.JWTToken.TokenIssuer, Configurations.JWTToken.TokenSubject);
            if(!validateResult) {
                return CreateErrorResponse(clientTokenMessage, StatusCodes.Status401Unauthorized);
            } 
            
            string email = req.Query["email"];
            // validate email address
            if (string.IsNullOrWhiteSpace(email))
            {
                return CreateErrorResponse($"Email is empty");
            }

            if (!email.IsValidEmailAddress())
            {
                return CreateErrorResponse($"Email {email} is invalid");
            }

            // only delete user from special domain
            if (!email.EndsWith(Configurations.AzureB2C.EmailTestDomain))
            {
                return CreateErrorResponse($"email {email} is invalid");
            }

            email = email.NormalizeEmail();
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
