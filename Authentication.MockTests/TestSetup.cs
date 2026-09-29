using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Authentication.MockTests
{
    /// <summary>
    /// Fixed, non-secret configuration for every test. Never reads local.settings.json.
    /// Every live service gets fake credentials and an unreachable endpoint, so a call that
    /// slips past validation fails to connect or authenticate instead of touching real data.
    /// </summary>
    public static class TestSetup
    {
        const string Unreachable = "https://127.0.0.1:9/";

        static readonly Dictionary<string, string> Values = new()
        {
            // Cognito / AWS (explicit credentials only; unknown region cannot resolve)
            { "CognitoKey", "test-key" }, { "CognitoSecret", "test-secret" },
            { "CognitoRegion", "test-nowhere-1" }, { "AWSS3MainRegion", "test-nowhere-1" },
            { "CognitoUrl", Unreachable }, { "AWSRestUrl", Unreachable }, { "AppleRedirectUrl", Unreachable },
            { "CognitoPoolId", "test-pool" }, { "CognitoClientId", "test-client" }, { "AWSRestCode", "test-code" },
            { "AWSS3MainBucket", "test-bucket" }, { "AWSS3MainPrefixPath", "test/" },
            // Cosmos
            { "DatabaseUrl", Unreachable }, { "DatabaseId", "test-db" },
            { "DatabaseMasterKey", "dGVzdGtleXRlc3RrZXl0ZXN0a2V5dGVzdGtleXRlc3Q=" }, { "PartitionKey", "partition" },
            // Azure AD B2C
            { "TenantName", "test-nowhere" }, { "TenantId", "00000000-0000-0000-0000-000000000000" },
            { "AdminClientSecret", "test-secret" }, { "TokenClientSecret", "test-secret" },
            // Storage, analytics, email, LLM
            { "MainStorageConnection", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;BlobEndpoint=" + Unreachable },
            { "UserStorageConnection", "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;BlobEndpoint=" + Unreachable },
            { "UserStorageContainerName", "test" },
            { "AnalyticsUrl", Unreachable }, { "AnalyticsToken", "test-token" },
            { "SendyUrl", Unreachable }, { "SendyKey", "test-key" }, { "ReoonKey", "test-key" },
            { "OpenAIKey", "test-key" }, { "OpenAIModel", "test-model" },
            // Tokens
            { "SignInKey", "0123456789abcdef0123456789abcdef" }, { "PasswordSecretKey", "test-secret" },
            { "DeepLinkKey", "test-key" },
            { "TokenIssuer", "test-issuer" }, { "TokenSubject", "test-subject" },
            { "EmailTestDomain", "bookbot.test" },
            // Social sign-in
            { "AppleClientIds", "com.bookbot.test.app;com.bookbot.test.web" }, { "AppleClientId", "TESTKEY123" },
            { "AppleTeamId", "TESTTEAM01" },
            { "GoogleClientIds", "123456789012-abc123def.apps.googleusercontent.com;123456789012-ghi456jkl.apps.googleusercontent.com;com.googleusercontent.apps.123456789012-mno789pqr" },
        };

        /// <summary>Throwaway P-256 key standing in for the Apple .p8 key (AppleSecret is its PKCS#8, base64)</summary>
        public static readonly ECDsa AppleKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        static TestSetup()
        {
            Values["AppleSecret"] = Convert.ToBase64String(AppleKey.ExportPkcs8PrivateKey());
            Configurations.Configuration = new ConfigurationBuilder().AddInMemoryCollection(Values).Build();

            // Apple/Google id tokens are verified against a local test key instead of the providers' published keys
            TokenService.OpenIdProvider.Apple.SigningKeys = _ => Task.FromResult(TestTokens.ProviderKeys());
            TokenService.OpenIdProvider.Google.SigningKeys = _ => Task.FromResult(TestTokens.ProviderKeys());
        }

        /// <summary>Ensures the test configuration is in place (runs once per test run).</summary>
        public static void UseTestConfiguration() { }

        public static HttpRequest Request(string query = "", string method = "GET")
        {
            var request = new DefaultHttpContext().Request;
            request.Method = method;
            request.QueryString = new QueryString(string.IsNullOrEmpty(query) ? "" : "?" + query);
            return request;
        }

        public static (int? Status, JObject Body) Read(IActionResult result)
        {
            var json = Assert.IsType<JsonResult>(result);
            return (json.StatusCode, JObject.FromObject(json.Value));
        }
    }
}
