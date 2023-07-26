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
using Authentication.Shared.Services.Responses;
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

        public interface IAWSRestApi
        {            
            [Post("/auth/signIn")]
            Task<AwsPasscode> RequestPasscode([Body(BodySerializationMethod.Serialized )] Dictionary<string, string> body);

            [Post("/auth/verify")]
            Task<AwsAPIResult> VerifyPasscode([Body(BodySerializationMethod.Serialized)] Dictionary<string, string> body);
        }

        private ICognitoRestApi cognitoRestApi;
        public static CognitoService Instance { get; } = new CognitoService();
        private AmazonCognitoIdentityProviderClient provider;

        private IAWSRestApi awsRestApi;
        private CognitoService()
        {
            cognitoRestApi = RestService.For<ICognitoRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri(Configurations.Cognito.CognitoUrl)
            });

            awsRestApi = RestService.For<IAWSRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri(Configurations.Cognito.AWSRestUrl)
            });

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

        public async Task<UserType> FindUserByEmail(string email)
        {
            var request = new ListUsersRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Filter = $"email = \"{email.ToLower()}\"",
            };
            var usersResponse = await provider.ListUsersAsync(request);
            if (usersResponse.Users.Count > 0)
            {
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => x.Name == "custom:authChallenge"));
                return user;
            }

            return null;
        }

        public async Task<UserType> FindUserByCustomId(string customId)
        {
            var request = new ListUsersRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Filter = $"preferred_username = \"{customId}\"",
            };
            var usersResponse = await provider.ListUsersAsync(request);
            if (usersResponse.Users.Count > 0)
            {
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => x.Name == "custom:authChallenge"));
                return user;
            }

            return null;
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
            if (usersResponse.Users.Count > 1)
            {
                Logger.Log?.LogError($"There are {usersResponse.Users.Count} duplicated {email} user");
                throw new Exception($"There are {usersResponse.Users.Count} duplicated {email} user");
            }
            else if (usersResponse.Users.Count == 1)
            {
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => x.Name == "custom:authChallenge"));
                return (true, user);
            }
            else
            {
                // then create cognito user
                var attributes = new List<AttributeType>();
                if(!string.IsNullOrWhiteSpace(name))
                {
                    attributes.Add(new AttributeType() { Name = "name", Value = name});
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    attributes.Add(new AttributeType() { Name = "custom:country", Value = country });
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    attributes.Add(new AttributeType() { Name = "custom:ipAddress", Value = ipAddress });
                }

                var userId = Guid.NewGuid().ToString();

                // set custom user id from b2c        
                attributes.Add(new AttributeType() { Name = "preferred_username", Value = userId });
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

                UserType newUser;
                try
                {
                    var createUserResponse = await provider.AdminCreateUserAsync(createRequest);
                    newUser = createUserResponse.User;
                }
                catch (UsernameExistsException ex)
                {
                    // TODO will remove later (after fixing from client)
                    Logger.Log?.LogError($"user name exist {ex.Message}");
                    // user exist in other request, just get it from cognito after few second
                    Task.Delay(5 * 1000).Wait();
                    usersResponse = await provider.ListUsersAsync(request);
                    newUser = usersResponse.Users.First();
                }

                // then change its password
                var changePasswordRequest = new AdminSetUserPasswordRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = newUser.Username,
                    Password = TokenService.GeneratePassword(email),
                    Permanent = true
                };

                await provider.AdminSetUserPasswordAsync(changePasswordRequest);

                // add cognito user into group new
                await UpdateUserGroup(newUser.Username, "new");

                // dont return passcode property to client
                newUser.Attributes.Remove(newUser.Attributes.Find(x => x.Name == "custom:authChallenge"));
                return (false, newUser);
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

        public async Task UpdateUserGroup(string id, string groupName)
        {
            var groupsResponse = await provider.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest
                {
                    Username = id,
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                }
            );

            var userAlreadyInGroup = false;
            if(groupsResponse.Groups.Count > 0)
            {
                foreach(var group in groupsResponse.Groups)
                {
                    if(group.GroupName == groupName)
                    {
                        userAlreadyInGroup = true;
                        continue;
                    }
                    else
                    {
                        await provider.AdminRemoveUserFromGroupAsync(new AdminRemoveUserFromGroupRequest
                        {
                            Username = id,
                            UserPoolId = Configurations.Cognito.CognitoPoolId,
                            GroupName = group.GroupName,
                        });
                    }
                }
            }

            if(userAlreadyInGroup)
            {
                return;
            }

            var request = new AdminAddUserToGroupRequest
            {
                Username = id,
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                GroupName = groupName,
            };

            await provider.AdminAddUserToGroupAsync(request);
        }

        public async Task<string> RequestPasscode(string email, string language, bool disableEmail = false, string appId = "")
        {
            if(string.IsNullOrWhiteSpace(language))
            {
                language = "en";
            }

            var parameters = new Dictionary<string, string>
                {
                    {"email", email },
                    {"code", Configurations.Cognito.AWSRestCode },
                    {"language", language },
                    {"disableEmail", disableEmail == true? "true": "false" }
                };

            if(!string.IsNullOrWhiteSpace(appId))
            {
                parameters["app_id"] = appId;
            }

            var response = await awsRestApi.RequestPasscode(parameters);

            return response.Passcode;
        }

        public async Task<bool> VerifyPasscode(string email, string passcode)
        {
            try
            {
                var response = await awsRestApi.VerifyPasscode(new Dictionary<string, string>
                {
                    {"email", email },
                    {"code", Configurations.Cognito.AWSRestCode },
                    {"passcode", passcode },
                }
            );

                return response.Message == "success";
            }
            catch (ApiException ex)
            {
                if(ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                {
                    throw ex;
                }
            }

            return false;
        }

        public async Task<string> GetCustomUserId(string id)
        {
            var response = await provider.AdminGetUserAsync(new AdminGetUserRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Username = id,
            });

            return response.UserAttributes.FirstOrDefault(x => x.Name == "preferred_username")?.Value;
        }

        public async Task<bool> DeleteUser(string id)
        {
            var response = await provider.AdminDeleteUserAsync(new AdminDeleteUserRequest {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Username = id,
            });

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }
    }
}

