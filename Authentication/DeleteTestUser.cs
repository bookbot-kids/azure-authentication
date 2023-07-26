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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;

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
            if (string.IsNullOrWhiteSpace(clientToken))
            {
                return CreateErrorResponse("client_token is missing", StatusCodes.Status401Unauthorized);
            }
            var (validateResult, clientTokenMessage, payload) = TokenService.ValidateClientToken(clientToken, Configurations.JWTToken.TokenClientSecret,
                 Configurations.JWTToken.TokenIssuer, Configurations.JWTToken.TokenSubject);
            if (!validateResult)
            {
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
            var user = await CognitoService.Instance.FindUserByEmail(email);
            if(user == null)
            {
                return CreateErrorResponse($"email {email} is invalid");
            }

            var userId = user.Username;
            log.LogInformation($"Delete user {userId}, {email}");

            var dataService = new DataService();

            // delete cosmos user
            await DeleteUserRecords(dataService, "User", null,
                new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", userId));

            // delete all data of this user
            foreach (var table in Configurations.Cosmos.UserTablesToClear)
            {
                await DeleteUserRecords(dataService, table, userId);
            }

            // delete cognito user
            await CognitoService.Instance.DeleteUser(userId);

            return CreateSuccessResponse();
        }

        private async Task DeleteUserRecords(DataService dataService, string table, string userId, QueryDefinition query = null)
        {
            if (query == null)
            {
                query = new QueryDefinition("select * from c where c.partition = @id").WithParameter("@id", userId);
            }

            var documents = await dataService.QueryDocuments(table, query);
            await documents.ParallelForEachAsync(
                async doc =>
                {
                    var id = doc.GetValue("id").Value<string>();
                    await dataService.DeleteById(table, id, userId, ignoreNotFound: true);
                }, maxDegreeOfParallelism: 64
             );
        }
    }
}
