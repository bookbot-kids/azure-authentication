﻿using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Services;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// Azure AD user. See <see href="https://docs.microsoft.com/en-us/previous-versions/azure/ad/graph/api/entity-and-complex-type-reference#user-entity">document</see> <br/>
    /// It contains information of AD user and support to manage user (CRUD) functions
    /// </summary>
    public class ADUser
    {
        #region Properties
        /// <summary>
        /// Gets or sets object id
        /// </summary>
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether account is enabled
        /// </summary>
        [JsonProperty("accountEnabled")]
        public bool AccountEnabled { get; set; }

        /// <summary>
        /// Gets or sets user type
        /// </summary>
        [JsonProperty("userType")]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets sign in name
        /// </summary>
        [JsonProperty("signInNames")]
        public List<SignInName> SignInNames { get; set; }

        /// <summary>
        /// Gets or sets country
        /// </summary>
        [JsonProperty("country")]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets IPAddress. It's streetAddress in azure AD
        /// </summary>
        [JsonProperty("streetAddress")]
        public string IPAddress { get; set; }

        /// <summary>
        /// Sign in name config
        /// </summary>
        public class SignInName
        {
            /// <summary>
            /// Gets or sets type
            /// </summary>
            [JsonProperty("type")]
            public string Type { get; set; } = "emailAddress";

            /// <summary>
            /// Gets or sets value
            /// </summary>
            [JsonProperty("value")]
            public string Value { get; set; }
        }

        /// <summary>
        /// Gets or sets password policies
        /// </summary>
        [JsonProperty("passwordPolicies")]
        public string PasswordPolicies { get; set; }

        #endregion

        #region Methods
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
        public static async Task<(bool result, ADUser user)> FindOrCreate(string email, string name = null, string country = null, string ipAddress = null)
        {
            // find user by email
            email = email.ToLower();
            var user = await FindByEmail(email);

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
                    Password = TokenService.GeneratePassword(email)
                },
                SignInNames = new List<CreateADUserParameters.SignInName>
                {
                    new CreateADUserParameters.SignInName
                {
                    Value = email
                }
                }
            };

            if(!string.IsNullOrWhiteSpace(country))
            {
                param.Country = country;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                param.IPAddress = ipAddress;
            }

            return (false, await AzureB2CService.Instance.CreateADUser(param));
        }

        public async Task Update(Dictionary<string, dynamic> param)
        {
            await AzureB2CService.Instance.UpdateADUser(ObjectId, param);
        }

        public async Task Delete()
        {
            await AzureMSGraphService.Instance.DeleteADUser(ObjectId);
        }

        public async Task SetEnable(bool enabled)
        {
            await AzureMSGraphService.Instance.UpdateADUser(ObjectId, new Dictionary<string, dynamic> {
                { "accountEnabled", enabled }           
            });
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
        /// Get group name of current user. Expect there is only one group
        /// </summary>
        /// <returns>Null or group name</returns>
        public async Task<string> GroupName()
        {
            var groupIds = await GroupIds();
            if(groupIds.Count > 0)
            {
                var group = await ADGroup.FindById(groupIds[0]);
                return group?.Name;
            }

            return null;
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
            if (groupdIds != null)
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
        public async Task<List<PermissionProperties>> GetPermissions(string groupName, List<string> definedTables)
        {
            var result = new List<PermissionProperties>();
            // create user if needed
            await CosmosService.Instance.CreateUser(ObjectId);

            var rolePermissions = await CosmosRolePermission.QueryByIdPermissions();
            List<Task<PermissionProperties>> tasks = new List<Task<PermissionProperties>>();
            foreach (var rolePermission in rolePermissions)
            {
                if(definedTables.Count > 0 && !definedTables.Contains(rolePermission.Table))
                {
                    continue;
                }
                // admin will have id-read-write permission for all tables
                if ("admin".EqualsIgnoreCase(groupName))
                {
                    tasks.Add(GetOrCreateAdminPermissions(rolePermission));
                    continue;
                }

                // only check for current group that user belongs
                if (!groupName.EqualsIgnoreCase(rolePermission.Role))
                {
                    continue;
                }

                tasks.Add(GetOrCreateUserPermissions(rolePermission));
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
        /// Get or create Cosmos permission for a user
        /// </summary>
        /// <param name="rolePermission">The role permission record</param>
        /// <returns>A permission class or null</returns>
        private async Task<PermissionProperties> GetOrCreateUserPermissions(CosmosRolePermission rolePermission)
        {
            var permission = await CosmosService.Instance.GetPermission(ObjectId, rolePermission.Table);
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
                    var updatedPermission = await CosmosService.Instance.ReplacePermission(ObjectId, rolePermission.Table,
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
            var adminPermission = await CosmosService.Instance.GetPermission(ObjectId, rolePermission.Table);
            if (adminPermission == null)
            {
                // create permission if not exist
                var newPermission = await CosmosService.Instance.CreatePermission(ObjectId, rolePermission.Table, false, rolePermission.Table, ObjectId);
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
       
        #endregion
    }
}
