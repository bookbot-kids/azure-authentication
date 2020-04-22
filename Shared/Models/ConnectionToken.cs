using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    public partial class ConnectionToken
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets from id
        /// </summary>
        [JsonProperty(PropertyName = "fromId")]
        public string FromId { get; set; }

        /// <summary>
        /// Gets or sets from email
        /// </summary>
        [JsonProperty(PropertyName = "fromEmail")]
        public string FromEmail { get; set; }

        /// <summary>
        /// Gets or sets from first name
        /// </summary>
        [JsonProperty(PropertyName = "fromFirstName")]
        public string FromFirstName { get; set; }

        /// <summary>
        /// Gets or sets from last name
        /// </summary>
        [JsonProperty(PropertyName = "fromLastName")]
        public string FromLastName { get; set; }

        /// <summary>
        /// Gets or sets email
        /// </summary>
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets to user id
        /// </summary>
        [JsonProperty(PropertyName = "toId")]
        public string ToId { get; set; }

        /// <summary>
        /// Gets or sets state
        /// Available states: "invited", "shared", "unshared"
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets first name
        /// </summary>
        [JsonProperty(PropertyName = "firstName")]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets last name
        /// </summary>
        [JsonProperty(PropertyName = "lastName")]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets type
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets child first name
        /// </summary>
        [JsonProperty(PropertyName = "childFirstName")]
        public string ChildFirstName { get; set; }

        /// <summary>
        /// Gets or sets child last name
        /// </summary>
        [JsonProperty(PropertyName = "childLastName")]
        public string ChildLastName { get; set; }

        /// <summary>
        /// Gets or sets permission
        /// </summary>
        [JsonProperty(PropertyName = "permission")]
        public string Permission { get; set; }

        /// <summary>
        /// Gets or sets token
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets viewed
        /// </summary>
        [JsonProperty(PropertyName = "viewed")]
        public bool? Viewed { get; set; }

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
        /// Gets or sets partition
        /// </summary>
        [JsonProperty(PropertyName = "partition")]
        public string Partition { get; set; }
    }
}
