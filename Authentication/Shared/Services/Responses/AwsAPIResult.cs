using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Services.Responses
{
    public class AwsAPIResult
    {
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}

