using Newtonsoft.Json;

namespace Authentication.Shared.Responses
{
    /// <summary>
    /// API result
    /// </summary>
    public class APIResult
    {
        /// <summary>
        /// Gets or sets medadata
        /// </summary>
        [JsonProperty(PropertyName = "odata.metadata")]
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether result is success
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public bool Value { get; set; }
    }
}
