using Newtonsoft.Json;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos profile model
    /// It contains profile properties and support to query, CRUD on profile model
    /// </summary>
    public partial class Profile
    {
        #region Properties
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

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
        /// Gets or sets date of birth
        /// </summary>
        [JsonProperty(PropertyName = "birthDate")]
        public long? DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets gender
        /// </summary>
        [JsonProperty(PropertyName = "gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Gets or sets last name
        /// </summary>
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets last name
        /// </summary>
        [JsonProperty(PropertyName = "recommendedProgram")]
        public string RecommendProgram { get; set; }

        /// <summary>
        /// Gets or sets partition
        /// </summary>
        [JsonProperty(PropertyName = "partition")]
        public string Partition { get; set; }

        #endregion

        #region Methods
        /// <summary>
        /// Get a profile by id
        /// </summary>
        /// <param name="userId">User id</param>
        /// <param name="profileId">Profile id</param>
        /// <returns>Profile record or null</returns>
        public static async Task<Profile> GetById(string userId, string profileId)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", profileId);
            var result = await CosmosService.Instance.QueryDocuments<Profile>("Profile", query, partition: userId);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Get list of profile by user id
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>List of profiles</returns>
        public static async Task<List<Profile>> GetByUserId(string userId)
        {
            var query = new QueryDefinition("select * from c where c.userId = @userId").WithParameter("@userId", userId);
            return await CosmosService.Instance.QueryDocuments<Profile>("Profile", query, partition: userId);
        }
        #endregion
    }
}
