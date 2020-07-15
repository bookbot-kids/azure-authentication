using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Requests;
using Authentication.Shared.Services;
using Authentication.Shared.Utils;
using Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD User
    /// It contains all the methods to manages AD user
    /// </summary>
    public partial class ADUser
    {
        /// <summary>
        /// Find user by id
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>ADUser class</returns>
        public static Task<ADUser> FindById(string id)
        {
            return AzureB2CService.Instance.GetUserById(id);
        }

        /// <summary>
        /// Find user by email
        /// </summary>
        /// <param name="email">email param</param>
        /// <returns>ADUser class</returns>
        public static Task<ADUser> FindByEmail(string email)
        {
            return AzureB2CService.Instance.GetADUserByEmail(email.ToLower());
        }

        /// <summary>
        /// Find or create a user
        /// If user already exists, then return result = true and user info
        /// Otherwise create a new user, then return result = false and user info
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="name">user name</param>
        /// <returns>(Result, ADUser). Result is true if user already exist, otherwise is false</returns>
        public static async Task<(bool result, ADUser user)> FindOrCreate(string email, string name = null)
        {
            // find user by email
            email = email.ToLower();
            var user = await AzureB2CService.Instance.GetADUserByEmail(email);

            // user already exist
            if (user != null)
            {
                return (true, user);
            }

            // create an user when it doesn't exist
            var param = new CreateADUserParameters
            {
                DisplayName = string.IsNullOrWhiteSpace(name) ? email : name,
                Profile = new CreateADUserParameters.PasswordProfile
                {
                    Password = HttpHelper.GeneratePassword(email)
                },
                    SignInNames = new List<CreateADUserParameters.SignInName>
                {
                    new CreateADUserParameters.SignInName
                {
                    Value = email
                }
                }
            };

            return (false, await AzureB2CService.Instance.CreateADUser(param));
        }

        /// <summary>
        /// Get group id list of current user 
        /// </summary>
        /// <returns>List of group id</returns>
        public Task<List<string>> GroupIds()
        {
            return AzureB2CService.Instance.GetUserGroups(ObjectId);
        }

        /// <summary>
        /// Change group for current user
        /// </summary>
        /// <param name="newGroupName"></param>
        /// <returns></returns>
        public async Task<bool> UpdateGroup(string newGroupName)
        {
            var group = await ADGroup.FindByName(newGroupName);
            var groupdIds = await GroupIds();
            if(groupdIds != null)
            {
                if (groupdIds.Count > 0)
                {
                    // remove user from all other groups
                    foreach (var id in groupdIds)
                    {
                        if (id != group.Id)
                        {
                            var oldGroup = await ADGroup.FindById(id);
                            if (oldGroup != null)
                            {
                                var removeResult = await oldGroup.RemoveUser(ObjectId);
                                if (!removeResult)
                                {
                                    Logger.Log?.LogError($"can not remove user {ObjectId} from group {oldGroup.Id}");
                                    return false;
                                }
                            }
                        }
                    }

                    // if user already in given group, then return success
                    if (groupdIds.FirstOrDefault(s => s == group.Id) != null)
                    {
                        return true;
                    }
                }
            }
            

            // otherwise, add user into new group
            var addResult = await group.AddUser(ObjectId);
            return addResult;
        }

        /// <summary>
        /// Get cosmos permission of this user
        /// </summary>
        /// <returns>List of permissions</returns>
        public async Task<List<PermissionProperties>> GetPermissions(string groupName)
        {
            var result = new List<PermissionProperties>();
            // create user if needed
            await DataService.Instance.CreateUser(ObjectId);

            var rolePermissions = await CosmosRolePermission.QueryByIdPermissions();
            List<Task<PermissionProperties>> tasks = new List<Task<PermissionProperties>>();
            foreach (var rolePermission in rolePermissions)
            {
                // admin will have id-read-write permission for all tables
                if("admin".EqualsIgnoreCase(groupName))
                {
                    tasks.Add(GetOrCreateAdminPermissions(rolePermission));
                    continue;
                }

                // only check for current group that user belongs
                if(!groupName.EqualsIgnoreCase(rolePermission.Role))
                {
                    continue;
                }

                tasks.Add(GetOrCreateUserPermissions(rolePermission));
            }

            // add shared permission
            var connections = await Connection.QueryByShareUser(ObjectId);
            foreach (var connection in connections)
            {
                // only process the accepted connection
                if (!"accepted".EqualsIgnoreCase(connection.Status))
                {
                    continue;
                }

                tasks.Add(GetOrCreateSharePermissions(connection));

                // add shared profile permission
                if (connection.Profiles != null && connection.Profiles.Count > 0)
                {
                    tasks.Add(GetOrCreateShareProfilePermissions(connection));
                }
            }

            await Task.WhenAll(tasks);
            foreach (var task in tasks)
            {
                if (task.Result != null)
                {
                    var data = task.Result;
                    result.Add(data);
                }
            }

            // remove duplicated
            result = result.GroupBy(p => new { p.ETag, p.Id, p.ResourceUri, p.LastModified, p.PermissionMode, p.SelfLink })
                        .Select(g => g.First()).ToList();

            return result;
        }

        /// <summary>
        /// Get or create cosmos permission for shared Profile table
        /// </summary>
        /// <param name="connection">The connection record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateShareProfilePermissions(Connection connection)
        {
            var permission = await connection.GetProfilePermission();
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await connection.CreateProfilePermission();
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogError($"error create profile permission ${ObjectId} ${connection.Table}");
                }
            }
            else
            {
                if ((connection.Permission.EqualsIgnoreCase("read") && permission.PermissionMode == PermissionMode.All)
                    || (connection.Permission.EqualsIgnoreCase("write") && permission.PermissionMode == PermissionMode.Read))
                {
                    // rolePermission is changed, need to update in cosmos
                    var updatedPermission = await connection.UpdateProfilePermission();
                    if (updatedPermission != null)
                    {
                        return updatedPermission;
                    }
                    else
                    {
                        Logger.Log?.LogError($"error update profile permission ${ObjectId} ${connection.Table}");
                    }
                }
                else
                {
                    return permission;
                }
            }

            return null;
        }

        /// <summary>
        /// Get or create Cosmos permission for a user
        /// </summary>
        /// <param name="rolePermission">The role permission record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateUserPermissions(CosmosRolePermission rolePermission)
        {
            var permission = await DataService.Instance.GetPermission(ObjectId, rolePermission.Table);
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await rolePermission.CreateCosmosPermission(ObjectId, rolePermission.Table, ObjectId);
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogError($"error create permission ${ObjectId} ${rolePermission.Table}");
                }
            }
            else
            {
                if ((rolePermission.Permission.EqualsIgnoreCase("id-read") && permission.PermissionMode == PermissionMode.All)
                    || (rolePermission.Permission.EqualsIgnoreCase("id-read-write") && permission.PermissionMode == PermissionMode.Read))
                {
                    // rolePermission is changed, need to update in cosmos
                    var updatedPermission = await DataService.Instance.ReplacePermission(ObjectId, rolePermission.Table,
                        rolePermission.Permission.EqualsIgnoreCase("id-read"), rolePermission.Table, partition: ObjectId);
                    if (updatedPermission != null)
                    {
                        return updatedPermission;
                    }
                    else
                    {
                        Logger.Log?.LogError($"error update permission ${ObjectId} ${rolePermission.Table}");
                    }
                }
                else
                {
                    return permission;
                }
            }

            return null;
        }

        /// <summary>
        /// Get or create Cosmos permission for an admin user
        /// </summary>
        /// <param name="rolePermission">The role permission record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateAdminPermissions(CosmosRolePermission rolePermission)
        {
            var adminPermission = await DataService.Instance.GetPermission(ObjectId, rolePermission.Table);
            if (adminPermission == null)
            {
                // create permission if not exist
                var newPermission = await DataService.Instance.CreatePermission(ObjectId, rolePermission.Table, false, rolePermission.Table, ObjectId);
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogWarning($"error create admin permission ${ObjectId} ${rolePermission.Table}");
                }
            }

            return adminPermission;
        }

        /// <summary>
        /// Get or create shared cosmos permission base on Connection record
        /// </summary>
        /// <param name="connection">The role permission record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateSharePermissions(Connection connection)
        {
            var permission = await connection.GetPermission();
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await connection.CreatePermission();
                if (newPermission != null)
                {
                    return newPermission;
                }
                else
                {
                    Logger.Log?.LogError($"error create permission ${ObjectId} ${connection.Table}");
                }
            }
            else
            {
                if ((connection.Permission.EqualsIgnoreCase("read") && permission.PermissionMode == PermissionMode.All)
                    || (connection.Permission.EqualsIgnoreCase("write") && permission.PermissionMode == PermissionMode.Read))
                {
                    // rolePermission is changed, need to update in cosmos
                    var updatedPermission = await connection.UpdatePermission();
                    if (updatedPermission != null)
                    {
                        return updatedPermission;
                    }
                    else
                    {
                        Logger.Log?.LogError($"error update permission ${ObjectId} ${connection.Table}");
                    }
                }
                else
                {
                    return permission;
                }
            }

            return null;
        }
    }
}
