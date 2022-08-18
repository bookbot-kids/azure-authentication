using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Services.Responses
{
    public class GoogleTokenResponse
    {
        [JsonProperty(PropertyName = "azp")]
        public string Azp { get; set; }

        [JsonProperty(PropertyName = "aud")]
        public string Aud { get; set; }

        [JsonProperty(PropertyName = "sub")]
        public string Sub { get; set; }

        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [JsonProperty(PropertyName = "exp")]
        public string Exp { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public string ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "email_verified")]
        public string Email_verified { get; set; }

        [JsonProperty(PropertyName = "access_type")]
        public string AccessType { get; set; }
    }
}

