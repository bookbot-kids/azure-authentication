using System.Collections.Generic;
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
                        Logger.Log?.LogWarning($"error create permission ${ObjectId} ${rolePermission.Table}");
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
                            Logger.Log?.LogWarning($"error update permission ${ObjectId} ${rolePermission.Table}");
                        }
                    }
                    else
                    {
                        result.Add(permission);
                    }
                }
            }
               
            return result;
        }
    }
}
