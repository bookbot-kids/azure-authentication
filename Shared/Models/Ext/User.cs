using System;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos user modal
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Get a user by id
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>User record or null</returns>
        public static async Task<User> GetById(string id)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var result = await DataService.Instance.QueryDocuments<User>("User", query, partition: id);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Search user by email
        /// </summary>
        /// <param name="email">User email</param>
        /// <returns>User record or null</returns>
        public static async Task<User> GetByEmail(string email)
        {
            if(email != null)
            {
                email = email.ToLower();
            }

            var query = new QueryDefinition("select * from c where LOWER(c.email) = @email").WithParameter("@email", email);
            var result = await DataService.Instance.QueryDocuments<User>("User", query, crossPartition: true);
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

            return await DataService.Instance.CreateOrUpdateDocument("User", Id, this, Partition);
        }
    }
}
