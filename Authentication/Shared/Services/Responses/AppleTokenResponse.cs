using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Services.Responses
{
    public class AppleTokenResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "id_token")]
        public string IdToken { get; set; }
    }
}

