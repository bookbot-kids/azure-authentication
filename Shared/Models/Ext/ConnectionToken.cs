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
            var result = await DataService.Instance.QueryDocuments<ConnectionToken>("ConnectionToken", query);
            return result.Count == 0 ? null : result[0];
        }

        public static async Task<ConnectionToken> GetByFromId(string id)
        {
            var query = new QueryDefinition("select * from c where c.from = @id").WithParameter("@id", id);
            var result = await DataService.Instance.QueryDocuments<ConnectionToken>("ConnectionToken", query);
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
                Partition = From;
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
                Logger.Log.LogError($"professional email {Email} not found");
                return;
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
            var profiles = await Profile.GetByUserId(Id);
            var profileIds = profiles.Select(s => s.Id).ToList();
            var connection = await Connection.QueryBy2Users(From, professionalUser.Id);
            if (connection == null)
            {
                var newConnection = new Connection()
                {
                    User1 = From,
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
        }

        private async Task ParentDeny(User professionalUser)
        {
            var connection = await Connection.QueryBy2Users(From, professionalUser.Id);
            if (connection != null)
            {
                connection.Status = "cancelled";
                connection.Partition = professionalUser.Id;
                await connection.CreateOrUpdate();
            }
        }

        #endregion

        #region professional
        public async Task ProfessionalProcess()
        {
            // only process state invited
            // ignore null
            string[] acceptedStates = { "invited"};
            if (State == null || !acceptedStates.Contains(State, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var parentUser = await User.GetByEmail(Email);
            if (parentUser == null)
            {
                Logger.Log.LogError($"professional email {Email} not found");
                return;
            }

            //create connection token for parent
            var connectionToken = await GetByFromId(parentUser.Id);
            if(connectionToken == null)
            {
                var newConnectionToken = new ConnectionToken()
                {
                    From = parentUser.Id,
                    Email = Email,
                    FirstName = FirstName,
                    LastName = LastName,
                    Type = "parent",
                    Partition = parentUser.Id,
                    ChildFirstName = ChildFirstName,
                    ChildLastName = ChildLastName,
                    State = "pending"
                };

                await newConnectionToken.CreateOrUpdate();
            }


            // create connection
            var connection = await Connection.QueryBy2Users(parentUser.Id, From);
            if (connection == null)
            {
                var newConnection = new Connection()
                {
                    User1 = parentUser.Id,
                    User2 = From,
                    Permission = Permission ?? "read",
                    Status = "pending",
                    Partition = From,
                    Table = "Report",
                    Profiles = {}
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

                connection.Partition = From;
                connection.Status = "accepted";
                await connection.CreateOrUpdate();
            }

        }

        #endregion
    }
}
