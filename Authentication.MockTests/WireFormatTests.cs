using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider.Model;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Authentication.MockTests
{
    /// <summary>
    /// Guards the JSON sent over the wire after the move to the isolated worker. In-process Functions
    /// serialized IActionResult bodies with Newtonsoft.Json and the MVC defaults (camelCase, explicit
    /// [JsonProperty] names honored); Program.cs keeps that with AddNewtonsoftJson().
    /// </summary>
    public class WireFormatTests
    {
        static ADToken Token() => new() { AccessToken = "at", RefreshToken = "rt", ExpiresIn = "3600" };

        static Shared.Models.User UserModel() =>
            JObject.Parse("{\"id\":\"u1\",\"email\":\"a@b.co\",\"firstName\":\"Pat\",\"type\":\"parent\"}").ToObject<Shared.Models.User>();

        static List<PermissionProperties> Permissions() => new()
        {
            JsonConvert.DeserializeObject<PermissionProperties>("{\"id\":\"p1\",\"permissionMode\":\"Read\",\"resource\":\"dbs/x/colls/y\",\"_token\":\"tok\"}"),
        };

        static UserType CognitoUser() => new()
        {
            Username = "user-1",
            Enabled = true,
            Attributes = new List<AttributeType> { new() { Name = "email", Value = "a@b.co" } },
        };

        // The shapes the functions return (RefreshToken, GetUserInfo, GetResourceTokens, CheckAccount)
        public static IEnumerable<object[]> Responses() => new[]
        {
            new object[] { "token", new JsonResult(new { success = true, token = Token() }) { StatusCode = 200 } },
            new object[] { "user", new JsonResult(new { success = true, user = UserModel() }) { StatusCode = 200 } },
            new object[] { "permissions", new JsonResult(new { success = true, permissions = Permissions(), group = "guest" }) { StatusCode = 200 } },
            new object[] { "cognito", new JsonResult(new { success = true, exist = true, user = CognitoUser(), passcode = "1234" }) { StatusCode = 200 } },
            new object[] { "error", BaseFunction.CreateErrorResponse("refresh_token is missing") },
        };

        [Theory]
        [MemberData(nameof(Responses))]
        public async Task MatchesInProcessNewtonsoftOutput(string name, IActionResult result)
        {
            var context = NewContext(services => services.AddMvcCore().AddNewtonsoftJson());
            var body = await Execute(result, context);

            var reference = JsonConvert.SerializeObject(((JsonResult)result).Value, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            });
            Assert.True(JToken.DeepEquals(JToken.Parse(reference), JToken.Parse(body)), $"{name}: {body}");
        }

        [Fact]
        public async Task TokenKeepsJsonPropertyNames()
        {
            var body = JObject.Parse(await Execute(new JsonResult(new { success = true, token = Token() }),
                NewContext(services => services.AddMvcCore().AddNewtonsoftJson())));

            Assert.Equal("at", (string)body["token"]["access_token"]);
            Assert.Equal("rt", (string)body["token"]["refresh_token"]);
            Assert.Equal("3600", (string)body["token"]["expires_in"]);
        }

        [Fact]
        public async Task DefaultSystemTextJsonWouldBreakTokenNames()
        {
            // Documents what AddNewtonsoftJson() in Program.cs protects against.
            var body = JObject.Parse(await Execute(new JsonResult(new { success = true, token = Token() }),
                NewContext(services => services.AddMvcCore())));

            Assert.Null(body["token"]["access_token"]);
        }

        static DefaultHttpContext NewContext(Action<IServiceCollection> configure)
        {
            var services = new ServiceCollection().AddLogging();
            configure(services);
            var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
            context.Response.Body = new MemoryStream();
            return context;
        }

        static async Task<string> Execute(IActionResult result, HttpContext context)
        {
            await result.ExecuteResultAsync(new ActionContext(context, new RouteData(), new ActionDescriptor()));
            context.Response.Body.Position = 0;
            return await new StreamReader(context.Response.Body).ReadToEndAsync();
        }
    }
}
