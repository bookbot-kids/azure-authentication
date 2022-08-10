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
            Task<ADToken> RequestPasscode([Body(BodySerializationMethod.Serialized )] Dictionary<string, string> body);

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

        public async Task<(bool, UserType)> FindOrCreateUser(string email, string name, string country, string ipAddress, ADUser adUser = null, bool shouldUpdateAdUser = true)
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
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => x.Name == "custom:authChallenge"));
                return (true, user);
            }
            else
            {
                var adUserExisting = false;
                if (adUser == null)
                {
                    // create AD User if needed
                    var (existing, newAdUser) = await ADUser.FindOrCreate(email, name, country, ipAddress);
                    adUser = newAdUser;
                    adUserExisting = existing;
                } else
                {
                    adUserExisting = true;
                }
               
                if (adUser == null)
                {
                    throw new Exception($"can not create ad user {email}");
                }

                // update new properties
                if(adUserExisting && shouldUpdateAdUser)
                {
                    var updateParams = new Dictionary<string, dynamic>();
                    if (!string.IsNullOrWhiteSpace(country))
                    {
                        updateParams["country"] = country;
                        adUser.Country = country;
                    }

                    if (!string.IsNullOrWhiteSpace(ipAddress))
                    {
                        updateParams["streetAddress"] = ipAddress;
                        adUser.IPAddress = ipAddress;
                    }

                    // enable account if needed
                    if (!adUser.AccountEnabled)
                    {
                        updateParams["accountEnabled"] = true;
                        adUser.AccountEnabled = true;
                    }

                    if (updateParams.Count > 0)
                    {
                        await adUser.Update(updateParams);
                    }
                }

                // then create cognito user
                var attributes = new List<AttributeType>();
                if(!string.IsNullOrWhiteSpace(name))
                {
                    attributes.Add(new AttributeType() { Name = "name", Value = name});
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    attributes.Add(new AttributeType() { Name = "custom:country", Value = country });
                } else if(!string.IsNullOrWhiteSpace(adUser.Country))
                {
                    attributes.Add(new AttributeType() { Name = "custom:country", Value = adUser.Country });
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    attributes.Add(new AttributeType() { Name = "custom:ipAddress", Value = ipAddress });
                } else if(!string.IsNullOrWhiteSpace(adUser.IPAddress))
                {
                    attributes.Add(new AttributeType() { Name = "custom:ipAddress", Value = adUser.IPAddress });
                }

                // set custom user id from b2c        
                attributes.Add(new AttributeType() { Name = "preferred_username", Value = adUser.ObjectId });
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
                var newUser = createUserResponse.User;

                // then change its password
                var changePasswordRequest = new AdminSetUserPasswordRequest
                {
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                    Username = newUser.Username,
                    Password = TokenService.GeneratePassword(email),
                    Permanent = true
                };

                await provider.AdminSetUserPasswordAsync(changePasswordRequest);
                

                if (!adUserExisting)
                {
                    // add cognito user into group new
                    await UpdateUserGroup(newUser.Username, "new");

                    if(shouldUpdateAdUser)
                    {
                        // add ad user into group new
                        var newGroup = await ADGroup.FindByName("new");
                        var addResult = await newGroup.AddUser(adUser.ObjectId);
                        if (!addResult)
                        {
                            throw new Exception($"can not add ad user {email} into new group");
                        }
                    }                   
                } else
                {
                    // add cognito user into group from b2c
                    var groupName = await adUser.GroupName();
                    if (!string.IsNullOrWhiteSpace(groupName))
                    {
                        await UpdateUserGroup(createUserResponse.User.Username, groupName);
                    } else
                    {
                        Logger.Log?.LogError($"user {email} does not have group");
                    }
                }

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

        public async Task RequestPasscode(string email)
        {
            await awsRestApi.RequestPasscode(new Dictionary<string, string>
                {
                    {"email", email },
                    {"code", Configurations.Cognito.AWSRestCode },
                }
            );
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
    }
}

