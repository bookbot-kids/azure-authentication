using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Extensions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos connection table model. This table defines how to generate <see href="https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data#resource-tokens"> cosmos resource tokens </see> when sharing records between users <br/>
    /// Contains all the model properties and support to query, crud action on cosmos model by calling cosmos service
    /// </summary>
    public class Connection
    {
        #region Properties    
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

        #endregion

        #region Methods
        /// <summary>
        /// Query connection by shared user id
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>List of Connection</returns>
        public static Task<List<Connection>> QueryByShareUser(string userId)
        {
            var query = new QueryDefinition("select * from c where c.user2 = @userId").WithParameter("@userId", userId);
            return DataService.Instance.QueryDocuments<Connection>("Connection", query, partition: userId);
        }

        /// <summary>
        /// Query connection by user 1 & user 2
        /// </summary>
        /// <param name="user1"> user 1</param>
        /// <param name="user2">user 2</param>
        /// <returns>Connection class</returns>
        public static async Task<Connection> QueryBy2Users(string user1, string user2)
        {
            var query = new QueryDefinition("select * from c where c.user1 = @user1 and c.user2 = @user2")
                .WithParameter("@user1", user1).WithParameter("@user2", user2);
            var result = await DataService.Instance.QueryDocuments<Connection>("Connection", query, partition: user2);
            return result.Count > 0 ? result[0] : null;
        }

        /// <summary>
        /// Create or update Connection
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<Connection> CreateOrUpdate()
        {
            if (Id == null)
            {
                Id = Guid.NewGuid().ToString();
            }

            if (Partition == null)
            {
                Partition = User2;
            }

            if (CreatedAt == default)
            {
                CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            }

            UpdatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            return DataService.Instance.CreateOrUpdateDocument("Connection", Id, this, Partition);
        }

        /// <summary>
        /// Get cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> GetPermission()
        {
            return DataService.Instance.GetPermission(User2, Table + "-shared-" + User1);
        }

        /// <summary>
        /// Create cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreatePermission()
        {
            return DataService.Instance.CreatePermission(User2, Table + "-shared-" + User1, IsReadOnly, Table, User1);
        }

        /// <summary>
        /// Update cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> UpdatePermission()
        {
            return DataService.Instance.ReplacePermission(User2, Table + "-shared-" + User1, IsReadOnly, Table, User1);
        }

        /// <summary>
        /// Get profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> GetProfilePermission()
        {
            return DataService.Instance.GetPermission(User2, "Profile-shared-" + User1);
        }

        /// <summary>
        /// Create profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> CreateProfilePermission()
        {
            return DataService.Instance.CreatePermission(User2, "Profile-shared-" + User1, IsReadOnly, "Profile", User1);
        }

        /// <summary>
        /// Update profile cosmos permission
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<PermissionProperties> UpdateProfilePermission()
        {
            return DataService.Instance.ReplacePermission(User2, "Profile-shared-" + User1, IsReadOnly, "Profile", User1);
        }
        #endregion
    }
}
