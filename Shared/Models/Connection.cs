using System.Collections.Generic;
using Extensions;
using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos connection modal
    /// Contains all the properties
    /// </summary>
    public partial class Connection
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets user 1
        /// </summary>
        [JsonProperty(PropertyName = "user1")]
        public string User1 { get; set; }

        /// <summary>
        /// Gets or sets user 2
        /// </summary>
        [JsonProperty(PropertyName = "user2")]
        public string User2 { get; set; }

        /// <summary>
        /// Gets or sets profiles
        /// </summary>
        [JsonProperty(PropertyName = "profiles")]
        public List<string> Profiles { get; set; }

        /// <summary>
        /// Gets or sets status
        /// Available statues: accepted, cancelled
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets permission
        /// </summary>
        [JsonProperty(PropertyName = "permission")]
        public string Permission { get; set; }

        /// <summary>
        /// Gets or sets table
        /// </summary>
        [JsonProperty(PropertyName = "table")]
        public string Table { get; set; }

        /// <summary>
        /// Gets or sets partition
        /// </summary>
        [JsonProperty(PropertyName = "partition")]
        public string Partition { get; set; }

        /// <summary>
        /// Gets or sets created at
        /// </summary>
        [JsonProperty(PropertyName = "createdAt")]
        public long CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets updated at
        /// </summary>
        [JsonProperty(PropertyName = "updatedAt")]
        public long UpdatedAt { get; set; }

        /// <summary>
        /// Gets whether permission is read only
        /// </summary>
        [JsonIgnore]
        public bool IsReadOnly
        {
            get { return Permission.EqualsIgnoreCase("read"); }
        }
    }
}
