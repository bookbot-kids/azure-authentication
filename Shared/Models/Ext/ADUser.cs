using System.Collections.Generic;
using System.Threading.Tasks;
using Authentication.Shared.Requests;
using Authentication.Shared.Services;
using Authentication.Shared.Utils;
using Microsoft.Azure.Cosmos;

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
        public Task<List<PermissionProperties>> GetPermissions()
        {
            return DataService.Instance.GetCosmosPermissions(ObjectId);
        }
    }
}
