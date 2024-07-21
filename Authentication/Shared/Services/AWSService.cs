using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Authentication.Shared.Library;
using Authentication.Shared.Models;
using Authentication.Shared.Services.Responses;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Refit;

namespace Authentication.Shared.Services
{
    public class AWSService
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
        public static AWSService Instance { get; } = new AWSService();
        private AmazonCognitoIdentityProviderClient provider;
        private AmazonS3Client amazonS3Client;

        private IAWSRestApi awsRestApi;
        private static List<string> secureAttributes = new List<string>() { "custom:authChallenge" };
        private AWSService()
        {
            cognitoRestApi = RestService.For<ICognitoRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri(Configurations.Cognito.CognitoUrl)
            });

            awsRestApi = RestService.For<IAWSRestApi>(new HttpClient(new HttpLoggingHandler())
            {
                BaseAddress = new Uri(Configurations.Cognito.AWSRestUrl)
            });

            var awsCredentials = new BasicAWSCredentials(Configurations.Cognito.CognitoKey, Configurations.Cognito.CognitoSecret);
            provider = new AmazonCognitoIdentityProviderClient(awsCredentials, RegionEndpoint.GetBySystemName(Configurations.Cognito.CognitoRegion));
            var regionEndpoint = RegionEndpoint.GetBySystemName(Configurations.Cognito.AWSS3MainRegion);
            amazonS3Client = new AmazonS3Client(Configurations.Cognito.CognitoKey, Configurations.Cognito.CognitoSecret, regionEndpoint);
        }

        #region Cognito
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

        public async Task<UserType> FindUserByEmail(string email, bool removeChallenge = true)
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
                if(removeChallenge)
                {
                    user.Attributes.Remove(user.Attributes.Find(x => secureAttributes.Contains(x.Name)));
                }
                
                return user;
            }

            return null;
        }

        public async Task<UserType> FindUserByPhone(string phone)
        {
            var request = new ListUsersRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Filter = $"phone_number = \"{phone}\"",
            };
            var usersResponse = await provider.ListUsersAsync(request);
            if (usersResponse.Users.Count > 0)
            {
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => secureAttributes.Contains(x.Name)));
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
                user.Attributes.Remove(user.Attributes.Find(x => secureAttributes.Contains(x.Name)));
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

        public async Task<(bool, UserType)> FindOrCreateUser(string email, string name, string country, string ipAddress, string phone = null, bool forceCreate = false)
        {
            email = email.ToLower();
            var listRequest = new ListUsersRequest
            {
                UserPoolId = Configurations.Cognito.CognitoPoolId,
                Filter = $"email = \"{email}\"",
            };

            Func<ListUsersResponse, Task<UserType>> createCallback = async (ListUsersResponse usersResponse) =>
            {
                // then create cognito user
                var attributes = new List<AttributeType>();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    attributes.Add(new AttributeType() { Name = "name", Value = name });
                }

                if (!string.IsNullOrWhiteSpace(country))
                {
                    attributes.Add(new AttributeType() { Name = "custom:country", Value = country });
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    attributes.Add(new AttributeType() { Name = "custom:ipAddress", Value = ipAddress });
                }

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    attributes.Add(new AttributeType() { Name = "phone_number", Value = phone });
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
                    Logger.Log?.LogError($"user name {phone} {email} exist {ex.Message}");
                    if (usersResponse != null)
                    {
                        // user exist in other request, just get it from cognito after few second
                        Task.Delay(5 * 1000).Wait();
                        usersResponse = await provider.ListUsersAsync(listRequest);
                        newUser = usersResponse.Users.First();
                    }
                    else
                    {
                        throw ex;
                    }
                } catch (Amazon.Runtime.Internal.HttpErrorResponseException ex)
                {
                    // TODO will remove later (after fixing from client)
                    if (ex.Message?.ToLower()?.Contains("alias entry already exists for a different username") == true && usersResponse != null)
                    {
                        Logger.Log?.LogError($"user name {phone} {email} exist {ex.Message}");
                        // user exist in other request, just get it from cognito after few second
                        Task.Delay(5 * 1000).Wait();
                        usersResponse = await provider.ListUsersAsync(listRequest);
                        newUser = usersResponse.Users.First();
                    }
                    else
                    {
                        Logger.Log?.LogError($"user name {phone} {email} has error {ex.Message}");
                        throw ex;
                    }
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
                newUser.Attributes.Remove(newUser.Attributes.Find(x => secureAttributes.Contains(x.Name)));
                return newUser;
            };

            if(forceCreate)
            {
                var newUser = await createCallback(null);
                return (false, newUser);
            }

            var usersResponse = await provider.ListUsersAsync(listRequest);
            if (usersResponse.Users.Count > 1)
            {
                Logger.Log?.LogError($"There are {usersResponse.Users.Count} duplicated {email} user");
                throw new Exception($"There are {usersResponse.Users.Count} duplicated {email} user");
            }
            else if (usersResponse.Users.Count == 1)
            {
                var user = usersResponse.Users.First();
                // dont return passcode property to client
                user.Attributes.Remove(user.Attributes.Find(x => secureAttributes.Contains(x.Name)));
                return (true, user);
            }
            else
            {
                var newUser = await createCallback(usersResponse);
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

        public async Task<string> GetUserGroup(string id) 
        {
            var groupsResponse = await provider.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest
                {
                    Username = id,
                    UserPoolId = Configurations.Cognito.CognitoPoolId,
                }
            );

            if(groupsResponse.Groups.Count > 0) 
            {
                return groupsResponse.Groups[0].GroupName;
            }

            return "";
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

        public async Task<string> RequestPasscode(string email, string language, bool disableEmail = false, string appId = "", string phone = "", string sendType = "email", bool returnPasscode = false)
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
                    {"disableEmail", disableEmail == true? "true": "false" },
                    {"returnPasscode", returnPasscode == true? "true": "false" },
                    {"sender_type", sendType },
                };

            if(!string.IsNullOrWhiteSpace(appId))
            {
                parameters["app_id"] = appId;
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                parameters["phone"] = phone;
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

        public string GetUserAttributeValue( UserType user, string name)
        {
            return user.Attributes.FirstOrDefault(x => x.Name == name)?.Value;
        }

        public void RemoveAttribute(UserType user, string attribute)
        {
            user.Attributes.Remove(user.Attributes.Find(x => x.Name == attribute));
        }

        #endregion

        #region S3
        public string GeneratePreSignedURL(string bucketName, string objectKey)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Verb = HttpVerb.PUT
            };

            return amazonS3Client.GetPreSignedURL(request);
        }
        #endregion
    }
}

