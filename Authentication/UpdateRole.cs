using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    /// <summary>
    /// Update user role (group) azure function
    /// This function uses to update user role. It also removes all the existing roles of user before assign to new role
    /// </summary>
    public class UpdateRole: BaseFunction
    {
        /// <summary>
        /// A http (Get, Post) method to update user role<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"auth_token": The authentication token to validate user role. Only admin can call this function</description></item>
        /// <item><description>"email": User email</description></item>
        /// <item><description>"role": user role to update. If user already has this role, then return success</description></item>
        /// </list> 
        /// This function will validate the email, then get ADUser by that email if it is valid.<br/>
        /// All the existing roles of user are removed, then assign user to the new role
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>Http success with code 200 if no error, otherwise return http error</returns> 
        [FunctionName("UpdateRole")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            if (!string.IsNullOrWhiteSpace(req.Query["refresh_token"]))
            {
                // get access token by refresh token
                var adToken = await ADAccess.Instance.RefreshToken(req.Query["refresh_token"]);
                if (adToken == null || string.IsNullOrWhiteSpace(adToken.AccessToken))
                {
                    return CreateErrorResponse("refresh token is invalid", StatusCodes.Status401Unauthorized);
                }

                // validate admin token
                var actionResult = await VerifyAdminToken(adToken.AccessToken);
                if (actionResult != null)
                {
                    return actionResult;
                }
            }
            else
            {
                // validate auth token
                var actionResult = await VerifyAdminToken(req.Query["auth_token"]);
                if (actionResult != null)
                {
                    return actionResult;
                }
            }

            // validate user email
            string email = req.Query["email"];

            // validate email address
            if (string.IsNullOrWhiteSpace(email) || !email.IsValidEmailAddress())
            {
                return CreateErrorResponse("Email is invalid");
            }

            // validate role parameter
            var group = await ADGroup.FindByName(req.Query["role"]);
            if (group == null)
            {
                return CreateErrorResponse("Role is invalid");
            }

            // replace space by + to correct because email contains "+" will be encoded by space, like "a+1@gmail.com" -> "a 1@gmail.com"
            email = email.Trim().Replace(" ", "+");

            string name = email.GetNameFromEmail();

            // create user if need
            var (_, user) = await ADUser.FindOrCreate(email, name);
            // there is an error when creating user
            if (user == null)
            {
                return CreateErrorResponse($"can not create user {email}", StatusCodes.Status500InternalServerError);
            }

            var result = await user.UpdateGroup(group.Name);

            string language = req.Query["language"];
            if(!string.IsNullOrWhiteSpace(language))
            {
                var cosmosUser = await User.GetById(user.ObjectId);
                if(cosmosUser == null)
                {
                    cosmosUser = new User
                    {
                        Id = user.ObjectId,
                        Email = email,
                        Language = language,
                    };
                    await cosmosUser.CreateOrUpdate();
                }
                else if(cosmosUser.Language != language)
                {
                    cosmosUser.Language = language;
                    await cosmosUser.CreateOrUpdate();
                }             
               
            }
          

            if (result)
            {
                return CreateSuccessResponse();
            }

            return CreateErrorResponse("can not add user into group");
        }
    }
}
