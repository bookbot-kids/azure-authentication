using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Microsoft.Extensions.Logging;
using Refit;

namespace Authentication.Shared.Services
{
    public class CognitoService
    {
        public interface ICognitoRestApi
        {
            [Headers("Content-Type: application/x-www-form-urlencoded")]
            [Post("/oauth2/token?grant_type=refresh_token")]
            Task<ADToken> GetAccessToken([AliasAs("client_id")] string clientId, [AliasAs("refresh_token")] string refreshToken);
        }

        private readonly HttpClient httpClient;
        private ICognitoRestApi cognitoRestApi;
        public static CognitoService Instance { get; } = new CognitoService();
        private AmazonCognitoIdentityProviderClient provider;
        private CognitoService()
        {
            httpClient = new HttpClient(new HttpLoggingHandler()) { BaseAddress = new Uri(Configurations.Cognito.CognitoUrl) };
            cognitoRestApi = RestService.For<ICognitoRestApi>(httpClient);

            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(Configurations.Cognito.CognitoKey, Configurations.Cognito.CognitoSecret);
            provider = new AmazonCognitoIdentityProviderClient(awsCredentials, RegionEndpoint.GetBySystemName(Configurations.Cognito.CognitoRegion));
        }

        public async Task SetAccountEable(string id, bool enabled)
        {
            if(enabled)
            {
                var request = new AdminEnableUserRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = id
                };
                await provider.AdminEnableUserAsync(request);
            } else
            {
                var request = new AdminDisableUserRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = id
                };

                await provider.AdminDisableUserAsync(request);
            }            
        }

        public async Task<AdminGetUserResponse> GetUserInfo(string id)
        {
            var request = new AdminGetUserRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Username = id
            };

            return await provider.AdminGetUserAsync(request);
        }

        public async Task<ADToken> GetAccessToken(string refreshToken)
        {
            try
            {
                return await cognitoRestApi.GetAccessToken(Configurations.Cognito.CognitoClientId, refreshToken);
            }
            catch (Exception ex)
            {
                Logger.Log?.LogError($"get cognito access token from refresh token {refreshToken} error {ex.Message}");
                return null;
            }
            
        }

        public async Task<(bool, string, string, string)> ValidateAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "token is missing", null, null);
            }

            var claimsIdentity = await TokenService.ValidateCognitoToken(token);
            if (claimsIdentity != null)
            {
                var idClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("username"));
                var groupClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("cognito:groups"));
                if (idClaim != null && groupClaim != null)
                {
                    return (true, null, idClaim.Value, groupClaim.Value);
                }

                return (false, "Can not find user with this token", null, null);
            }

            return (false, "Token is invalid", null, null);
        }

        public async Task<(bool, UserType)> FindOrCreateUser(string email, string name, string country, string ipAddress)
        {
            email = email.ToLower();
            var request = new ListUsersRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Filter = $"email = \"{email}\"",
            };

            var usersResponse = await provider.ListUsersAsync(request);
            if(usersResponse.Users.Count > 0 )
            {
                return (true, usersResponse.Users.First());
            }
            else
            {
                var attributes = new List<AttributeType>();
                if(!string.IsNullOrWhiteSpace(name))
                {
                    attributes.Add(new AttributeType() { Name = "name", Value = name});
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    attributes.Add(new AttributeType() { Name = "locale", Value = country });
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    attributes.Add(new AttributeType() { Name = "address", Value = ipAddress });
                }

                attributes.Add(new AttributeType() { Name = "email", Value = email });

                // create new user with temp password
                var createRequest = new AdminCreateUserRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = email,
                    UserAttributes = attributes,
                    TemporaryPassword = TokenService.GeneratePassword(Guid.NewGuid().ToString()),
                    MessageAction = MessageActionType.SUPPRESS,
                };
                var createUserResponse = await provider.AdminCreateUserAsync(createRequest);

                // then change its password
                var changePasswordRequest = new AdminSetUserPasswordRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = createUserResponse.User.Username,
                    Password = TokenService.GeneratePassword(Guid.NewGuid().ToString()),
                    Permanent = true
                };

                await provider.AdminSetUserPasswordAsync(changePasswordRequest);

                return (false, createUserResponse.User);
            }
        }

        public async Task UpdateUser(string id, Dictionary<string, string> attributes, bool setEable = false)
        {
            if(attributes.Count > 0)
            {
                var list = attributes.Select(x => new AttributeType { Name = x.Key, Value = x.Value }).ToList();
                var request = new AdminUpdateUserAttributesRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = id,
                    UserAttributes = list,
                };

                await provider.AdminUpdateUserAttributesAsync(request);
            }
            
            if(setEable)
            {
                await SetAccountEable(id, true);
            }
        }

        public async Task AddUserToGroup(string id, string groupName)
        {
            var request = new AdminAddUserToGroupRequest
            {
                Username = id,
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                GroupName = groupName,
            };

            await provider.AdminAddUserToGroupAsync(request);
        }
    }
}

