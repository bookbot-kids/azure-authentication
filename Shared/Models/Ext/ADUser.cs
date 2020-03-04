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
        /// Get cosmos permission of this user
        /// </summary>
        /// <returns>List of permissions</returns>
        public async Task<List<PermissionProperties>> GetPermissions(string groupName)
        {
            var result = new List<PermissionProperties>();
            // create user if needed
            await DataService.Instance.CreateUser(ObjectId);

            var rolePermissions = await CosmosRolePermission.QueryByIdPermissions();
            foreach (var rolePermission in rolePermissions)
            {
                // admin will have id-read-write permission for all tables
                if("admin".EqualsIgnoreCase(groupName))
                {
                    await AddAdminPermissions(result, rolePermission);
                    continue;
                }

                // only check for current group that user belongs
                if(!groupName.EqualsIgnoreCase(rolePermission.Role))
                {
                    continue;
                }

                var permission = await DataService.Instance.GetPermission(ObjectId, rolePermission.Table);
                if(permission == null)
                {
                    // create permission if not exist
                    var newPermission = await rolePermission.CreateCosmosPermission(ObjectId, rolePermission.Table, ObjectId);
                    if (newPermission != null)
                    {
                        result.Add(newPermission);
                    }
                    else
                    {
                        Logger.Log?.LogError($"error create permission ${ObjectId} ${rolePermission.Table}");
                    }
                } else
                {
                    if ((rolePermission.Permission.EqualsIgnoreCase("id-read") && permission.PermissionMode == PermissionMode.All)
                        || (rolePermission.Permission.EqualsIgnoreCase("id-read-write") && permission.PermissionMode == PermissionMode.Read))
                    {
                        // rolePermission is changed, need to update in cosmos
                        var updatedPermission = await DataService.Instance.ReplacePermission(ObjectId, rolePermission.Table,
                            rolePermission.Permission.EqualsIgnoreCase("id-read"), rolePermission.Table, partition: ObjectId);
                        if (updatedPermission != null)
                        {
                            result.Add(updatedPermission);
                        }
                        else
                        {
                            Logger.Log?.LogError($"error update permission ${ObjectId} ${rolePermission.Table}");
                        }
                    }
                    else
                    {
                        // prevent duplicate
                        if (!result.Any(e => e.Id == permission.Id && e.ResourceUri == permission.ResourceUri
                         && e.ETag == permission.ETag && e.LastModified == permission.LastModified
                         && e.PermissionMode == permission.PermissionMode))
                        {
                            result.Add(permission);
                        }
                    }
                }
            }

            await AddSharePermissions(result);
            return result;
        }

        private async Task AddSharePermissions(List<PermissionProperties> result)
        {
            var connections = await Connection.QueryByShareUser(ObjectId);
            foreach (var connection in connections)
            {
                // only process the accepted connection
                if(!"accepted".EqualsIgnoreCase(connection.Status))
                {
                    continue;
                }

                var permission = await connection.GetPermission();
                if (permission == null)
                {
                    // create permission if not exist
                    var newPermission = await connection.CreatePermission();
                    if (newPermission != null)
                    {
                        result.Add(newPermission);
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
                            result.Add(updatedPermission);
                        }
                        else
                        {
                            Logger.Log?.LogError($"error update permission ${ObjectId} ${connection.Table}");
                        }
                    }
                    else
                    {
                        result.Add(permission);
                    }
                }

                if (connection.Profiles != null)
                {
                    foreach (var profile in connection.Profiles)
                    {
                        await AddProfilePermission(result, connection, profile);
                    }
                }
            }
        }

        private async Task AddProfilePermission(List<PermissionProperties> result, Connection connection, string profileId)
        {
            var permission = await connection.GetProfilePermission(profileId);
            if (permission == null)
            {
                // create permission if not exist
                var newPermission = await connection.CreateProfilePermission(profileId);
                if (newPermission != null)
                {
                    result.Add(newPermission);
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
                    var updatedPermission = await connection.UpdateProfilePermission(profileId);
                    if (updatedPermission != null)
                    {
                        result.Add(updatedPermission);
                    }
                    else
                    {
                        Logger.Log?.LogError($"error update profile permission ${ObjectId} ${connection.Table}");
                    }
                }
                else
                {
                    result.Add(permission);
                }
            }
        }

        private async Task AddAdminPermissions(List<PermissionProperties> result, CosmosRolePermission rolePermission)
        {
            var adminPermission = await DataService.Instance.GetPermission(ObjectId, rolePermission.Table);
            if (adminPermission == null)
            {
                // create permission if not exist
                var newPermission = await DataService.Instance.CreatePermission(ObjectId, rolePermission.Table, false, rolePermission.Table, ObjectId);
                if (newPermission != null)
                {
                    result.Add(newPermission);
                }
                else
                {
                    Logger.Log?.LogWarning($"error create admin permission ${ObjectId} ${rolePermission.Table}");
                }
            }
            else
            {
                // prevent duplicate
                if (!result.Any(e => e.Id == adminPermission.Id && e.ResourceUri == adminPermission.ResourceUri
                 && e.ETag == adminPermission.ETag && e.LastModified == adminPermission.LastModified
                 && e.PermissionMode == adminPermission.PermissionMode))
                {
                    result.Add(adminPermission);
                }

            }
        }
    }
}
