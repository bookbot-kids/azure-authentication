using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Services.Responses
{
	public class DeepLink
	{
        [JsonProperty(PropertyName = "shortLink")]
        public string ShortLink { get; set; }

        [JsonProperty(PropertyName = "previewLink")]
        public string PreviewLink { get; set; }
    }
}

