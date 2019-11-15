using System;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Utils;
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
    [Obsolete("This Azure function is deprecated. Will remove later")]
    public static class UpdateRole
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

            // validate auth token
            var actionResult = await HttpHelper.VerifyAdminToken(req.Query["auth_token"]);
            if (actionResult != null)
            {
                return actionResult;
            }

            // validate user email
            var email = req.Query["email"];
            if (string.IsNullOrWhiteSpace(email))
            {
                return HttpHelper.CreateErrorResponse("email is missing");
            }

            // get user by email
            var user = await ADUser.FindByEmail(email);
            if (user == null)
            {
                return HttpHelper.CreateErrorResponse("email not exist");
            }

            // validate role parameter
            var group = await ADGroup.FindByName(req.Query["role"]);
            if (group == null)
            {
                return HttpHelper.CreateErrorResponse("Role is invalid");
            }

            // get all roles (groups) of user
            var groupdIds = await user.GroupIds();
            if (groupdIds?.Count > 0)
            {
                // remove user from all other groups
                foreach (var id in groupdIds)
                {
                    if (id != group.Id)
                    {
                        var oldGroup = await ADGroup.FindById(id);
                        if (oldGroup != null)
                        {
                            var removeResult = await oldGroup.RemoveUser(user.ObjectId);
                            if (!removeResult)
                            {
                                return HttpHelper.CreateErrorResponse($"can not remove user from group {id}");
                            }
                        }                        
                    }
                }

                // if user already in given group, then return success
                if (groupdIds.FirstOrDefault(s => s == group.Id) != null)
                {
                    return HttpHelper.CreateSuccessResponse();
                }
            }

            // otherwise, add user into new group
            var addResult = await group.AddUser(user.ObjectId);

            if (!addResult)
            {
                return HttpHelper.CreateErrorResponse("can not add user into group");
            }

            // return success
            return HttpHelper.CreateSuccessResponse();
        }
    }
}
