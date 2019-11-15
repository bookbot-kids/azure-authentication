using System.Collections.Generic;
using Authentication.Shared.Models;
using Newtonsoft.Json;

namespace Authentication.Shared.Responses
{
    /// <summary>
    /// B2c Group list response 
    /// </summary>
    public class GroupsResponse
    {
        /// <summary>
        /// Gets or sets metadata of group
        /// </summary>
        [JsonProperty(PropertyName = "odata.metadata")]
        public string Metadata { get; set; }

        /// <summary>
        /// Gets or sets group list
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public List<ADGroup> Groups { get; set; }
    }
}
