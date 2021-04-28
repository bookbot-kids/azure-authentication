using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos user model <br/>
    /// It contains user properties and support to query, CRUD on user model
    /// </summary>
    public partial class User
    {
        #region Properties
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
        /// Gets or sets customer type
        /// </summary>
        [JsonProperty(PropertyName = "customer")]
        public string Customer { get; set; }

        /// <summary>
        /// Gets or sets accent
        /// </summary>
        [JsonProperty(PropertyName = "accent")]
        public string Accent { get; set; } = "i18n";

        /// <summary>
        /// Gets or sets language
        /// </summary>
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; } = "en";

        /// <summary>
        /// Gets or sets language
        /// </summary>
        [JsonProperty(PropertyName = "appRating")]
        public int AppRating { get; set; } = 0;

        #endregion

        #region Methods

        /// <summary>
        /// Get a user by id
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>User record or null</returns>
        public static async Task<User> GetById(string id)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var result = await CosmosService.Instance.QueryDocuments<User>("User", query, partition: id);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Search user by email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>User record or null</returns>
        public static async Task<User> GetByEmail(string email)
        {
            if (email != null)
            {
                email = email.ToLower();
            }

            var query = new QueryDefinition("select * from c where LOWER(c.email) = @email").WithParameter("@email", email);
            var result = await CosmosService.Instance.QueryDocuments<User>("User", query, crossPartition: true);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Create or update a user record
        /// </summary>
        /// <returns>User record</returns>
        public async Task<User> CreateOrUpdate()
        {
            if (Id == null)
            {
                Id = Guid.NewGuid().ToString();
            }

            if (Partition == null)
            {
                Partition = Id;
            }

            if (CreatedAt == default)
            {
                CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            }

            UpdatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            return await CosmosService.Instance.CreateOrUpdateDocument("User", Id, this, Partition);
        }

        #endregion
    }
}
