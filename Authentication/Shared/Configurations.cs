using Microsoft.Extensions.Configuration;

namespace Authentication.Shared
{
    /// <summary>
    /// Configurations class
    /// This class contains all the configurations constants
    /// </summary>
    public static class Configurations
    {
        /// <summary>
        /// Gets or sets App configuration
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// Cosmos db constants
        /// </summary>
        public static class Cosmos
        {
            /// <summary>
            /// The resource token expiration
            /// </summary>
            public const int ResourceTokenExpiration = 86400;

            /// <summary>
            /// The partition key
            /// </summary>
            public static string PartitionKey => Configuration["PartitionKey"];

            /// <summary>
            /// Default partition
            /// </summary>
            public static string DefaultPartition => Configuration["DefaultPartition"];

            /// <summary>
            /// Cosmos database id
            /// </summary>
            public static string DatabaseId => Configuration["DatabaseId"];

            /// <summary>
            /// Cosmos master key
            /// </summary>
            public static string DatabaseMasterKey => Configuration["DatabaseMasterKey"];

            /// <summary>
            /// Cosmos database url
            /// </summary>
            public static string DatabaseUrl => Configuration["DatabaseUrl"];

            /// <summary>
            /// The permissions that can create
            /// </summary>
            public static readonly string[] AcceptedPermissions =
            {
            "read",
            "read-write",
            "id-read",
            "id-read-write"
            };

            public static string[] UserTablesToClear => (Configuration["UserTablesToClear"] ?? "").Split(",");

            public static bool IgnoreConnectionPermission => bool.Parse(Configuration["IgnoreConnectionPermission"] ?? "false");
        }

        /// <summary>
        /// Azure B2C constants
        /// </summary>
        public static class AzureB2C
        {
            /// <summary>
            /// Authentication url
            /// </summary>
            public const string MicorsoftAuthUrl = "https://login.microsoftonline.com";

            /// <summary>
            /// Azure Graph resource url
            /// </summary>
            public const string GraphResource = "https://graph.windows.net";

            /// <summary>
            /// Password grant type
            /// </summary>
            public const string GrantTypePassword = "password";

            /// <summary>
            /// Credentials grant type
            /// </summary>
            public const string GrantTypeCredentials = "client_credentials";

            /// <summary>
            /// Refresh token grant type
            /// </summary>
            public const string GrantTypeRefreshToken = "refresh_token";

            /// <summary>
            /// Bearer authentication header
            /// </summary>
            public const string BearerAuthentication = "Bearer ";

            /// <summary>
            /// Token type
            /// </summary>
            public const string TokenType = "token ";

            /// <summary>
            /// Id token type
            /// </summary>
            public const string IdTokenType = "id_token ";

            /// <summary>
            /// Tenant id
            /// </summary>
            public static string TenantId => Configuration["TenantId"];

            /// <summary>
            /// Tenant name
            /// </summary>
            public static string TenantName => Configuration["TenantName"];

            /// <summary>
            /// admin client key
            /// </summary>
            public static string AdminClientId => Configuration["AdminClientId"];

            /// <summary>
            /// Admin secret key
            /// </summary>
            public static string AdminClientSecret => Configuration["AdminClientSecret"];

            /// <summary>
            /// Secret key to generate password
            /// </summary>
            public static string PasswordSecretKey => Configuration["PasswordSecretKey"];

            /// <summary>
            /// The admin group name
            /// </summary>
            public static readonly string AdminGroup = "admin";

            /// <summary>
            /// The guest group name
            /// </summary>
            public static readonly string GuestGroup = "guest";

            /// <summary>
            /// The new group name
            /// </summary>
            public static readonly string NewGroup = "new";

            /// <summary>
            /// Password prefix to by pass the password complexity requirements
            /// </summary>
            public static string PasswordPrefix => Configuration["PasswordPrefix"];

            /// <summary>
            /// B2C resource url
            /// </summary>
            public static readonly string B2CUrl = $"https://{TenantName}.b2clogin.com/{TenantName}.onmicrosoft.com/oauth2/v2.0";

            /// <summary>
            /// ROPC auth policy
            /// </summary>
            public static string AuthPolicy => Configuration["AuthPolicy"];

            /// <summary>
            /// Sign in sign up policy
            /// </summary>
            public static string SignInSignUpPolicy => Configuration["SignInSignUpPolicy"];

            /// <summary>
            /// B2C client id
            /// </summary>
            public static string B2CClientId => Configuration["B2CClientId"];

            public static readonly string GraphApiUrl = "https://graph.microsoft.com/v1.0";

            /// <summary>
            /// Test email domain
            /// </summary>
            public static string EmailTestDomain => Configuration["EmailTestDomain"] ?? "[######]"; // default is invalid domain
        }

        /// <summary>
        /// JWT Token
        /// </summary>
        public static class JWTToken
        {
            /// <summary>
            /// Mobile client secret key
            /// </summary>
            public static string TokenClientSecret => Configuration["TokenClientSecret"];

            /// <summary>
            /// Token issuer
            /// </summary>
            public static string TokenIssuer => Configuration["TokenIssuer"];

            /// <summary>
            /// Sub domain
            /// </summary>
            public static string TokenSubject => Configuration["TokenSubject"];

            public static string SignInKey => Configuration["SignInKey"];
        }

        public static class Storage
        {
            /// <summary>
            /// Storage connection string
            /// </summary>
            public static string UserStorageConnection => Configuration["UserStorageConnection"];

            public static string UserStorageContainerName => Configuration["UserStorageContainerName"];

            public static string MainStorageConnection => Configuration["MainStorageConnection"];
        }

        public static class Analytics
        {
            /// <summary>
            /// analytics server url
            /// </summary>
            public static string AnalyticsUrl => Configuration["AnalyticsUrl"];

            public static string AnalyticsToken => Configuration["AnalyticsToken"];

            public static string SendyUrl => Configuration["SendyUrl"];
            public static string SendyKey => Configuration["SendyKey"];
            public static string SendyRegisteredListId => Configuration["SendyRegisteredListId"];
            public static string SendyTipsListId => Configuration["SendyTipsListId"];

            public static string ReoonKey => Configuration["ReoonKey"];
        }

        public static class Cognito
        {
            public static string CognitoClientId => Configuration["CognitoClientId"];

            public static string CognitoUrl => Configuration["CognitoUrl"];

            public static string CognitoPoolId => Configuration["CognitoPoolId"];

            public static string CognitoRegion => Configuration["CognitoRegion"];

            public static string CognitoKey => Configuration["CognitoKey"];

            public static string CognitoSecret => Configuration["CognitoSecret"];

            public static string AWSRestUrl => Configuration["AWSRestUrl"];

            public static string AWSRestCode => Configuration["AWSRestCode"];

            public static string AWSS3MainRegion => Configuration["AWSS3MainRegion"];

            public static string AWSS3MainBucket => Configuration["AWSS3MainBucket"];

            public static string AWSS3MainPrefixPath => Configuration["AWSS3MainPrefixPath"];
        }

        public static class Google
        {
            public static string[] GoogleClientIds => Configuration["GoogleClientIds"].Split(";");
        }

        public static class Apple
        {
            public static string AppleClientId => Configuration["AppleClientId"];
            public static string AppleTeamId => Configuration["AppleTeamId"];
            public static string AppleServiceId => Configuration["AppleServiceId"];
            public static string AppleAppId => Configuration["AppleAppId"];
            public static string AppleSecret => Configuration["AppleSecret"];
            public static string AppleRedirectUrl => Configuration["AppleRedirectUrl"];
            public static string[] AppleClientIds => Configuration["AppleClientIds"].Split(";");
        }

        public static class Whatsapp
        {
            public static string PlaceholderEmail => Configuration["WhatsappPlaceholderEmail"];
        }

        public static class DeepLink
        {
            public static string Key => Configuration["DeepLinkKey"];
        }

        public static class OpenAI
        {
            public static string Key => Configuration["OpenAIKey"];
            public static string Model => Configuration["OpenAIModel"];
        }
    }
}
