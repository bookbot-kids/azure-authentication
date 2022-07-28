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
using System.Collections.Generic;

namespace Authentication
{
    public class DeleteUser: BaseFunction
    {
        [FunctionName("DeleteUser")]
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

            // then validate refresh token
            string refreshToken = req.Query["refresh_token"];
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return CreateErrorResponse("refresh_token is missing");
            }

            // get access token by refresh token
            var adToken = await ADAccess.Instance.RefreshToken(refreshToken);
            if(adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
            {
                return CreateErrorResponse($"refresh_token is invalid: {refreshToken} ", StatusCodes.Status401Unauthorized);
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

            email = email.NormalizeEmail();

            var adUser = await ADUser.FindByEmail(email);
            if(adUser == null)
            {
                return CreateErrorResponse($"Can not find user {email}");
            }

            var userId = adUser.ObjectId;
            log.LogInformation($"Delete user {userId}, {email}");

            await adUser.SetEnable(false);

            // delete cosmos user
            var dataService = new DataService();
            await DeleteUserRecords(dataService, "User", null, 
                new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", userId));

            foreach(var table in Configurations.Cosmos.UserTablesToClear) {
                await DeleteUserRecords(dataService, table, userId);
            }

            try
            {
                await SendDeleteEvent(email);
            }
            catch (Exception ex)
            {
                if (!(ex is TimeoutException))
                {
                    log.LogError($"Send delete analytics error {ex.Message}");
                }
            }

            return CreateSuccessResponse();
        }

        private async Task DeleteUserRecords(DataService dataService, string table, string userId, QueryDefinition query = null) {
            if(query == null) {
                query = new QueryDefinition("select * from c where c.partition = @id").WithParameter("@id", userId);
            }
            
            var documents = await dataService.QueryDocuments(table, query);
            await documents.ParallelForEachAsync(
                async doc =>
                {
                    await DeleteRecord(dataService, table, doc);
                }, maxDegreeOfParallelism: 64
             );
        }

        private async Task DeleteRecord(DataService dataService, string table, JObject doc) {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            doc["updatedAt"] = new JValue(milliseconds);
            doc["deletedAt"] = new JValue(milliseconds);
            await dataService.SaveDocument(table, doc);
        }

        private async Task SendDeleteEvent(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            // log event for new user
            var body = new Dictionary<string, object>
                {
                    {
                        "activeCampaign", new Dictionary<string, string>
                            {
                                {"eventType", "event" },
                                {"eventName", "Delete Account"},
                                {"email", email },
                                {"eventdata", email }
                            }
                    },
                };

            // don't need to wait for this event, just make it timeout after few seconds
            var task = AnalyticsService.Instance.SendEvent(body);
            await HttpHelper.TimeoutAfter(task, TimeSpan.FromSeconds(7));
        }
    }
}
