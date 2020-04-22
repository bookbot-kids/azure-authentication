using System;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    public partial class ConnectionToken
    {
        public static async Task<ConnectionToken> GetById(string id)
        {
            var query = new QueryDefinition("select * from c where c.id = @id").WithParameter("@id", id);
            var result = await DataService.Instance.QueryDocuments<ConnectionToken>("ConnectionToken", query, crossPartition: true);
            return result.Count == 0 ? null : result[0];
        }

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
        public async Task ParentProcess()
        {
            // only process state invited, shared, unshared
            // ignore pending or null
            string[] acceptedStates = { "invited", "shared", "unshared" };
            if(State == null || !acceptedStates.Contains(State, StringComparer.OrdinalIgnoreCase))
            {
                Logger.Log?.LogWarning($"invalid state {State}");
                return;
            }

            var professionalUser = await User.GetByEmail(Email);
            if(professionalUser == null)
            {
                Logger.Log.LogError($"professional email {Email} not found. Create a new one");
                // create professional ad user
                var (_, adUser) = await ADUser.FindOrCreate(Email);
                // add professional user to new group
                var newGroup = await ADGroup.FindByName("new");
                var addResult = await newGroup.AddUser(adUser.ObjectId);
                if (!addResult)
                {
                    Logger.Log.LogError($"can not add user {Email} into new group");
                    return;
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
            if(connectionToken != null)
            {
                connectionToken.ToId = FromId;
                connectionToken.State = "accepted";
                await connectionToken.CreateOrUpdate();
            } else
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
                    FromLastName = FromLastName
                };

                await newConnectionToken.CreateOrUpdate();
            }

        }

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
        public async Task ProfessionalProcess()
        {
            // only process state invited, unshared
            // ignore null
            string[] acceptedStates = { "invited", "unshared"};
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
                // add parent user to new group
                var newGroup = await ADGroup.FindByName("new");
                var addResult = await newGroup.AddUser(adUser.ObjectId);
                if (!addResult)
                {
                    Logger.Log.LogError($"can not add user {Email} into new group");
                    return;
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
            if(canUpdateParent)
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
        }
}
