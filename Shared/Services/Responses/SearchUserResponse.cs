using System.Collections.Generic;
using Authentication.Shared.Models;
using Newtonsoft.Json;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Search user response
    /// </summary>
    public class SearchUserResponse
    {
        /// <summary>
        /// Gets or sets ad user list
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public List<ADUser> Values { get; set; }
    }
}
