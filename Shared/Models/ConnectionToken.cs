using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Library;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Cosmos ConnectionToken model, uses to share records between 2 users.
    /// To share records, it needs get the partition key of the shared record and set the permission via <see href="https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data">resource token</see> 
    /// Contains all the model properties and support to query, CRUD the ConnectionToken model by calling cosmos service
    /// </summary>
    public class ConnectionToken
    {
        #region Properties
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
        /// Gets or sets isFromParent
        /// </summary>
        [JsonProperty(PropertyName = "isFromParent")]
        public bool? IsFromParent { get; set; }

        /// <summary>
        /// Gets or sets partition
        /// </summary>
        [JsonProperty(PropertyName = "partition")]
        public string Partition { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Get record by id
        /// </summary>
        /// <param name="id">record id</param>
        /// <returns>ConnectionToken or null</returns>
        public static async Task<ConnectionToken> GetById(string id)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var result = await DataService.Instance.QueryDocuments<ConnectionToken>("ConnectionToken", query, crossPartition: true);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Get record from user & email
        /// </summary>
        /// <param name="fromId">From user id</param>
        /// <param name="email">user email</param>
        /// <returns>ConnectionToken or null</returns>
        public static async Task<ConnectionToken> GetFrom(string fromId, string email)
        {
            var query = new QueryDefinition("select * from c where c.fromId = @id and c.email = @email")
                .WithParameter("@id", fromId).WithParameter("@email", email);
            var result = await DataService.Instance.QueryDocuments<ConnectionToken>("ConnectionToken", query, partition: fromId);
            return result.Count == 0 ? null : result[0];
        }

        /// <summary>
        /// Create or update Connection
        /// </summary>
        /// <returns>Permission class</returns>
        public Task<ConnectionToken> CreateOrUpdate()
        {
            if (Id == null)
            {
                Id = Guid.NewGuid().ToString();
            }

            if (Partition == null)
            {
                Partition = FromId;
            }

            if (CreatedAt == default)
            {
                CreatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            }

            UpdatedAt = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();

            return DataService.Instance.CreateOrUpdateDocument("ConnectionToken", Id, this, Partition);
        }

        #region parents
        /// <summary>
        /// This function can handle the changes from ConnectionToken of parent
        /// It only accepts invited, shared, unshared states
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task ParentProcess()
        {
            // only process state invited, shared, unshared
            // ignore pending or null
            string[] acceptedStates = { "invited", "shared", "unshared" };
            if (State == null || !acceptedStates.Contains(State, StringComparer.OrdinalIgnoreCase))
            {
                Logger.Log?.LogWarning($"invalid state {State}");
                return;
            }

            var professionalUser = await User.GetByEmail(Email);
            if (professionalUser == null)
            {
                Logger.Log.LogError($"professional email {Email} not found. Create a new one");
                // create professional ad user
                var (_, adUser) = await ADUser.FindOrCreate(Email);
                var groups = await adUser.GroupIds();
                if (groups == null || groups.Count == 0)
                {
                    // add professional user to new group if needed
                    var newGroup = await ADGroup.FindByName("new");
                    var addResult = await newGroup.AddUser(adUser.ObjectId);
                    if (!addResult)
                    {
                        Logger.Log.LogError($"can not add user {Email} into new group");
                        return;
                    }
                }


                // create professional cosmos user
                professionalUser = new User
                {
                    Id = adUser.ObjectId,
                    Email = Email,
                    FirstName = FirstName,
                    LastName = LastName,
                    Type = "professional",
                    Partition = adUser.ObjectId
                };

                await professionalUser.CreateOrUpdate();
            }

            switch (State.ToLower())
            {
                // parent invite same as parent accept professional invitation
                case "invited":
                case "shared":
                    await ParentAccepted(professionalUser);
                    break;
                case "unshared":
                    await ParentDeny(professionalUser);
                    break;
            }
        }

        /// <summary>
        /// Process when parent accepts the invitation
        /// It creates new Connection record for sharing a table
        /// </summary>
        /// <param name="professionalUser"></param>
        /// <returns>Nothing</returns>
        private async Task ParentAccepted(User professionalUser)
        {
            var profiles = await Profile.GetByUserId(FromId);
            var profileIds = profiles.Select(s => s.Id).ToList();

            // update connect to accept
            var connection = await Connection.QueryBy2Users(FromId, professionalUser.Id);
            if (connection == null)
            {
                var newConnection = new Connection()
                {
                    User1 = FromId,
                    User2 = professionalUser.Id,
                    Permission = Permission ?? "read",
                    Status = "accepted",
                    Profiles = profileIds,
                    Partition = professionalUser.Id,
                    Table = "Report"
                };

                await newConnection.CreateOrUpdate();
            }
            else
            {
                if (connection.Permission == null)
                {
                    connection.Permission = "read";
                }

                if (connection.Table == null)
                {
                    connection.Table = "Report";
                }

                if (connection.Profiles == null || connection.Profiles.Count == 0)
                {
                    connection.Profiles = profileIds;
                }

                connection.Partition = professionalUser.Id;
                connection.Status = "accepted";
                await connection.CreateOrUpdate();
            }

            // update connection token of professional to accepted
            var connectionToken = await GetFrom(professionalUser.Id, FromEmail);
            if (connectionToken != null)
            {
                connectionToken.ToId = FromId;
                connectionToken.State = "accepted";
                connectionToken.IsFromParent = IsFromParent;
                await connectionToken.CreateOrUpdate();
            }
            else
            {
                var childFirstName = profiles.Count > 0 ? profiles[0].FirstName : FirstName;
                var childLastName = profiles.Count > 0 ? profiles[0].LastName : LastName;
                var newConnectionToken = new ConnectionToken()
                {
                    ToId = FromId,
                    Email = FromEmail,
                    State = "accepted",
                    FirstName = FirstName,
                    LastName = LastName,
                    ChildFirstName = childFirstName,
                    ChildLastName = childLastName,
                    FromEmail = professionalUser.Email,
                    FromId = professionalUser.Id,
                    Type = "professional",
                    FromFirstName = FromFirstName,
                    FromLastName = FromLastName,
                    IsFromParent = IsFromParent
                };

                await newConnectionToken.CreateOrUpdate();
            }

        }

        /// <summary>
        /// Process when parent denies the invitation
        /// It will change the Connection record into cancelled or deny state
        /// </summary>
        /// <param name="professionalUser"></param>
        /// <returns>Nothing</returns>
        private async Task ParentDeny(User professionalUser)
        {
            var connection = await Connection.QueryBy2Users(FromId, professionalUser.Id);
            if (connection != null)
            {
                connection.Status = "cancelled";
                connection.Partition = professionalUser.Id;
                await connection.CreateOrUpdate();
            }

            // update connection token of professional to deny
            var connectionToken = await GetFrom(professionalUser.Id, FromEmail);
            if (connectionToken != null)
            {
                connectionToken.ToId = FromId;
                connectionToken.State = "deny";
                await connectionToken.CreateOrUpdate();
            }
        }

        #endregion

        #region professional
        /// <summary>
        /// This function can handle the changes from ConnectionToken of professional
        /// It only accepts invited, unshared states
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task ProfessionalProcess()
        {
            // only process state invited, unshared
            // ignore null
            string[] acceptedStates = { "invited", "unshared" };
            if (State == null || !acceptedStates.Contains(State, StringComparer.OrdinalIgnoreCase))
            {
                Logger.Log?.LogWarning($"invalid state {State}");
                return;
            }

            var parentUser = await User.GetByEmail(Email);
            if (parentUser == null)
            {
                Logger.Log.LogInformation($"parentUser email {Email} not found. Create a new one");
                // create parent ad user
                var (_, adUser) = await ADUser.FindOrCreate(Email);
                var groups = await adUser.GroupIds();
                if (groups == null || groups.Count == 0)
                {
                    // add parent user to new group if needed
                    var newGroup = await ADGroup.FindByName("new");
                    var addResult = await newGroup.AddUser(adUser.ObjectId);
                    if (!addResult)
                    {
                        Logger.Log.LogError($"can not add user {Email} into new group");
                        return;
                    }
                }


                // create parent cosmos user
                parentUser = new User
                {
                    Id = adUser.ObjectId,
                    Email = Email,
                    FirstName = FirstName,
                    LastName = LastName,
                    Type = "parent",
                    Partition = adUser.ObjectId
                };

                await parentUser.CreateOrUpdate();

            }

            switch (State.ToLower())
            {
                // parent invite same as parent accept professional invitation
                case "invited":
                    await ProfessionalInvite(parentUser);
                    break;
                case "unshared":
                    await ProfessionalUnshare(parentUser);
                    break;
            }

        }

        /// <summary>
        /// Process when professional invites a parent
        /// </summary>
        /// <param name="parentUser">Parent user</param>
        /// <returns>Nothing</returns>
        private async Task ProfessionalInvite(User parentUser)
        {
            if (FromEmail == null)
            {
                Logger.Log.LogError($"from email is missing");
                return;
            }

            //create connection token for parent
            var connectionToken = await GetFrom(parentUser.Id, FromEmail);
            var canUpdateParent = true;
            if (connectionToken == null)
            {
                connectionToken = new ConnectionToken();
            }

            // Create or update parent token with information
            if (canUpdateParent)
            {
                connectionToken.FromId = parentUser.Id;
                connectionToken.Email = FromEmail;
                connectionToken.FromEmail = Email;
                connectionToken.FirstName = FirstName;
                connectionToken.LastName = LastName;
                connectionToken.Type = "parent";
                connectionToken.ChildFirstName = ChildFirstName;
                connectionToken.ChildLastName = ChildLastName;
                connectionToken.Permission = Permission;
                connectionToken.State = "pending";
                connectionToken.FromFirstName = FromFirstName;
                connectionToken.FromLastName = FromLastName;
                connectionToken.IsFromParent = IsFromParent;
                await connectionToken.CreateOrUpdate();
            }

            // create connection
            var connection = await Connection.QueryBy2Users(parentUser.Id, FromId);
            if (connection == null)
            {
                var newConnection = new Connection()
                {
                    User1 = parentUser.Id,
                    User2 = FromId,
                    Permission = Permission ?? "read",
                    Status = "pending",
                    Partition = FromId,
                    Table = "Report",
                    Profiles = { }
                };

                await newConnection.CreateOrUpdate();
            }
            //else
            //{
            //    if (connection.Permission == null)
            //    {
            //        connection.Permission = "read";
            //    }

            //    if (connection.Table == null)
            //    {
            //        connection.Table = "Report";
            //    }

            //    connection.Partition = FromId;
            //    connection.Status = "accepted";
            //    await connection.CreateOrUpdate();
            //}
        }

        /// <summary>
        /// Process when professional unshared the sharing connection with parent
        /// </summary>
        /// <param name="parentUser">Parent user</param>
        /// <returns>Nothing</returns>
        private async Task ProfessionalUnshare(User parentUser)
        {
            Logger.Log?.LogInformation($"unshare professional {FromEmail} and parent {parentUser.Email}");
            var connection = await Connection.QueryBy2Users(parentUser.Id, FromId);
            if (connection != null)
            {
                connection.Status = "cancelled";
                connection.Partition = FromId;
                await connection.CreateOrUpdate();
            }

            // update connection token of parent to deny
            var connectionToken = await GetFrom(parentUser.Id, FromEmail);
            if (connectionToken != null)
            {
                connectionToken.State = "deny";
                await connectionToken.CreateOrUpdate();
            }
        }

        #endregion

        #endregion
    }
}
