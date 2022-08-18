using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Services.Responses
{
    public class AwsPasscode: AwsAPIResult
    {
        [JsonProperty(PropertyName = "passcode")]
        public string Passcode { get; set; }
    }
}

