using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    public partial class User
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets email
        /// </summary>
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

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
        /// Gets or sets organisation name
        /// </summary>
        [JsonProperty(PropertyName = "organisationName")]
        public string OrganisationName { get; set; }

        /// <summary>
        /// Gets or sets gateway
        /// </summary>
        [JsonProperty(PropertyName = "gateway")]
        public string Gateway { get; set; }

        /// <summary>
        /// Gets or sets subscription expired at
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionExpiredAt")]
        public long? SubscriptionExpiredAt { get; set; }

        /// <summary>
        /// Gets or sets city
        /// </summary>
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        /// <summary>
        /// Gets or sets region
        /// </summary>
        [JsonProperty(PropertyName = "region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets country
        /// </summary>
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets is EU
        /// </summary>
        [JsonProperty(PropertyName = "isEU")]
        public bool? IsEU { get; set; }

        /// <summary>
        /// Gets or sets last sign in ip
        /// </summary>
        [JsonProperty(PropertyName = "lastSignInIP")]
        public string LastSignInIP { get; set; }

        /// <summary>
        /// Gets or sets partition
        /// </summary>
        [JsonProperty(PropertyName = "partition")]
        public string Partition { get; set; }
    }
}
