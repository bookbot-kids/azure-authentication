using System;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Authentication
{
    /// <summary>
    /// Create role permission azure function
    /// This function uses to create cosmos user and permission
    /// It return http success if there is no error, otherwise return http error
    /// </summary>
    public static class CreateRolePermission
    {
        /// <summary>
        /// A http (Get, Post) method to create role and permission in cosmos.<br/>
        /// Parameters:<br/>
        /// <list type="bullet">
        /// <item><description>"auth_token": The authentication token to validate user role. Only admin can call this function</description></item>
        /// <item><description>"role": cosmos role</description></item>
        /// <item><description>"table": cosmos table name</description></item>
        /// </list> 
        /// "Role" and "table" parameters must not be existed at the same time.<br/>
        /// If parameter "role" does exist, then look up the cosmos RolePermission table by this role and create cosmos user and permissions
        /// If parameter "table" does exist, then look up the cosmos RolePermission table by this table and create cosmos user and permissions
        /// Only "read" and "read-write" permissions in RolePermission table are processed. 
        /// </summary>
        /// <param name="req">HttpRequest type. It does contains parameters, headers...</param>
        /// <param name="log">The logger instance</param>
        /// <returns>Http success with code 200 if no error, otherwise return http error</returns>   
        [FunctionName("CreateRolePermission")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Logger.Log = log;

            // validate auth token, make sure only admin can call this function
            var actionResult = await HttpHelper.VerifyAdminToken(req.Query["auth_token"]);
            if (actionResult != null)
            {
                return actionResult;
            }

            // validate table & role
            string table = req.Query["table"];
            string role = req.Query["role"];

            // Role or table must be existed
            if (string.IsNullOrWhiteSpace(table) && string.IsNullOrWhiteSpace(role))
            {
                return HttpHelper.CreateErrorResponse("must enter table or role");
            }

            // Only role or table exist at a time
            if (!string.IsNullOrWhiteSpace(table) && !string.IsNullOrWhiteSpace(role))
            {
                return HttpHelper.CreateErrorResponse("only enter table or role");
            }

            // create role & permission by role
            if (!string.IsNullOrWhiteSpace(role))
            {
                await CreateRolePermissionForRoleAsync(role.ToLower());
            }
            else
            {
                // Validate table name in predefine list. Table name must be case sensitive, but the query parameter is not
                var tables = await CosmosRolePermission.GetAllTables();                
                var index = tables.FindIndex(t => t.Equals(table, StringComparison.OrdinalIgnoreCase));
                if (index >= 0 && index < tables.Count())
                {
                    // Create role and permission by table
                    await CreateRolePermissionForTableAsync(tables[index]);
                }
                else
                {
                    return HttpHelper.CreateErrorResponse($"Invalid table {table}");
                }
            }

            return HttpHelper.CreateSuccessResponse();
        }

        /// <summary>
        /// Create User (Role) and Permission in cosmos from input role
        /// </summary>
        /// <param name="role">user role</param>
        /// <returns>Task async</returns>
        private static async Task CreateRolePermissionForRoleAsync(string role)
        {
            var rolePermissions = await CosmosRolePermission.QueryByRole(role);

            // Create User with the role
            var userId = role;
            await CosmosRolePermission.CreateCosmosUser(userId);

            foreach (var item in rolePermissions)
            {
                // only process "read", "read-write" permissions
                if (!Configurations.Cosmos.AcceptedPermissions.Contains(item.Permission, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // create permission for this user role
                await item.CreateCosmosPermission(userId, item.Table);
            }
        }

        /// <summary>
        /// Create User (Role) and Permission in cosmos from input table
        /// </summary>
        /// <param name="table">table name</param>
        /// <returns>Task async</returns>
        private static async Task CreateRolePermissionForTableAsync(string table)
        {
            var rolePermissions = await CosmosRolePermission.QueryByTable(table);
            foreach (var item in rolePermissions)
            {
                // only process "read", "read-write" permissions
                if (!Configurations.Cosmos.AcceptedPermissions.Contains(item.Permission, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Create User with the role
                var userId = item.Role.ToLower();
                await CosmosRolePermission.CreateCosmosUser(userId);

                // Then create permission for that role
                await item.CreateCosmosPermission(userId, table);
            }
        }
    }
}
