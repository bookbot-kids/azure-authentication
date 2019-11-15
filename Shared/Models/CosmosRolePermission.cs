using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Role and permission
    /// This class is mapping from cosmos RolePermission table
    /// </summary>
    public partial class CosmosRolePermission
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets table
        /// </summary>
        [JsonProperty(PropertyName = "table")]
        public string Table { get; set; }

        /// <summary>
        /// Gets or sets role
        /// </summary>
        [JsonProperty(PropertyName = "role")]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets permission
        /// </summary>
        [JsonProperty(PropertyName = "permission")]
        public string Permission { get; set; }
    }
}
