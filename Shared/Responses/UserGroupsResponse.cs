using System.Collections.Generic;
using Newtonsoft.Json;

namespace Authentication.Shared.Responses
{
    /// <summary>
    /// User group response
    /// </summary>
    public class UserGroupsResponse
    {
        /// <summary>
        /// Gets or sets medadata
        /// </summary>
        [JsonProperty("odata.metadata")]
        public string OdataMetadata { get; set; }

        /// <summary>
        /// Gets or sets group id list
        /// </summary>
        [JsonProperty("value")]
        public List<string> GroupIds { get; set; }
    }
}
