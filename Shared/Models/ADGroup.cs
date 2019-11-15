using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD Group
    /// This class contains all the defined groups in b2c
    /// </summary>
    public sealed partial class ADGroup
    {
        /// <summary>
        /// Gets or sets object type
        /// </summary>
        [JsonProperty(PropertyName = "odata.type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets id of group
        /// </summary>
        [JsonProperty(PropertyName = "objectId")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets name of group
        /// </summary>
        [JsonProperty(PropertyName = "displayName")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description of group
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
}
