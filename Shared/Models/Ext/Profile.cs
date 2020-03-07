using System.Collections.Generic;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    public partial class Profile
    {
        public static async Task<Profile> GetById(string userId, string profileId)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", profileId);
            var result = await DataService.Instance.QueryDocuments<Profile>("Profile", query, partition: userId);
            return result.Count == 0 ? null : result[0];
        }

        public static async Task<List<Profile>> GetByUserId(string userId)
        {
            var query = new QueryDefinition("select * from c where c.userId = @userId").WithParameter("@userId", userId);
            return await DataService.Instance.QueryDocuments<Profile>("Profile", query, partition: userId);
        }
    }
}