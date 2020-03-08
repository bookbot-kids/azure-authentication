using System.Threading.Tasks;
using Authentication.Shared.Services;
using Microsoft.Azure.Cosmos;

namespace Authentication.Shared.Models
{
    public partial class User
    {
        public static async Task<User> GetById(string id)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var result = await DataService.Instance.QueryDocuments<User>("User", query, partition: id);
            return result.Count == 0 ? null : result[0];
        }

        public static async Task<User> GetByEmail(string email)
        {
            var query = new QueryDefinition("select * from c where c.email = @email").WithParameter("@email", email);
            var result = await DataService.Instance.QueryDocuments<User>("User", query, crossPartition: true);
            return result.Count == 0 ? null : result[0];
        }
    }
}
