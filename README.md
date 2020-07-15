# azure-authentication

## Setup
- Create a file `local.settings.json` in the root project, copy from `sample_local.settings.json` file
- Create b2c tenant https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-tenant, then link the subscription into b2c tenant (step 7 in url)
- Register admin b2c application and grant permissions to manage directory https://docs.microsoft.com/bs-latn-ba/azure/active-directory-b2c/ropc-custom?tabs=applications Then fill the `AdminClientId` & `AdminClientSecret` in `local.settings.json` with created application
- Register client b2c application to manage user authentication https://docs.microsoft.com/en-us/azure/active-directory-b2c/ropc-custom?tabs=applications then fill the `B2CClientId` in `local.settings.json` with created application
- Setup custom policy https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-overview-custom.
    + Replace {yourtenant} with your tenant name in sample policies in folder `B2C-policies`.
    + Replace [your url to custom html] to your custom html url from a storage 
    + Replace  {Your_ProxyIdentityExperienceFramework} with your b2c ProxyIdentityExperienceFramework app id in TrustFrameworkExtensions.xml
    + Replace  {Your_IdentityExperienceFramework} with your b2c IdentityExperienceFramework in app id in TrustFrameworkExtensions.xml
- Setup cosmos SQL database and fill the key into `local.settings.json` https://docs.microsoft.com/en-us/azure/cosmos-db/create-cosmosdb-resources-portal
- Go to Azure b2c portal and create 3 default groups with following name: "new", "admin", "guest" https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal
- After publish these functions, go to azure function portal to add environment variables from local.settings.json https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings#settings
- Go to function manages to set code for all functions, we will use param code=[function_key] to call APIs https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook?tabs=csharp#obtaining-keys

API Documentation:

<a name='assembly'></a>
# Authentication

## Contents

- [ADAccess](#T-Authentication-Shared-ADAccess 'Authentication.Shared.ADAccess')
  - [#ctor()](#M-Authentication-Shared-ADAccess-#ctor 'Authentication.Shared.ADAccess.#ctor')
  - [masterToken](#F-Authentication-Shared-ADAccess-masterToken 'Authentication.Shared.ADAccess.masterToken')
  - [Instance](#P-Authentication-Shared-ADAccess-Instance 'Authentication.Shared.ADAccess.Instance')
  - [GetAccessToken(email,password)](#M-Authentication-Shared-ADAccess-GetAccessToken-System-String,System-String- 'Authentication.Shared.ADAccess.GetAccessToken(System.String,System.String)')
  - [GetMasterKey()](#M-Authentication-Shared-ADAccess-GetMasterKey 'Authentication.Shared.ADAccess.GetMasterKey')
  - [RefreshToken(token)](#M-Authentication-Shared-ADAccess-RefreshToken-System-String- 'Authentication.Shared.ADAccess.RefreshToken(System.String)')
  - [ValidateAccessToken(token)](#M-Authentication-Shared-ADAccess-ValidateAccessToken-System-String- 'Authentication.Shared.ADAccess.ValidateAccessToken(System.String)')
  - [ValidateClientToken(token)](#M-Authentication-Shared-ADAccess-ValidateClientToken-System-String- 'Authentication.Shared.ADAccess.ValidateClientToken(System.String)')
  - [ValidateIdToken(token)](#M-Authentication-Shared-ADAccess-ValidateIdToken-System-String- 'Authentication.Shared.ADAccess.ValidateIdToken(System.String)')
- [ADGroup](#T-Authentication-Shared-Models-ADGroup 'Authentication.Shared.Models.ADGroup')
  - [Description](#P-Authentication-Shared-Models-ADGroup-Description 'Authentication.Shared.Models.ADGroup.Description')
  - [Id](#P-Authentication-Shared-Models-ADGroup-Id 'Authentication.Shared.Models.ADGroup.Id')
  - [Name](#P-Authentication-Shared-Models-ADGroup-Name 'Authentication.Shared.Models.ADGroup.Name')
  - [Type](#P-Authentication-Shared-Models-ADGroup-Type 'Authentication.Shared.Models.ADGroup.Type')
  - [AddUser(userId)](#M-Authentication-Shared-Models-ADGroup-AddUser-System-String- 'Authentication.Shared.Models.ADGroup.AddUser(System.String)')
  - [FindById(id)](#M-Authentication-Shared-Models-ADGroup-FindById-System-String- 'Authentication.Shared.Models.ADGroup.FindById(System.String)')
  - [FindByName(name)](#M-Authentication-Shared-Models-ADGroup-FindByName-System-String- 'Authentication.Shared.Models.ADGroup.FindByName(System.String)')
  - [GetAllGroups()](#M-Authentication-Shared-Models-ADGroup-GetAllGroups 'Authentication.Shared.Models.ADGroup.GetAllGroups')
  - [GetOrCreateAdminPermission(table)](#M-Authentication-Shared-Models-ADGroup-GetOrCreateAdminPermission-System-String- 'Authentication.Shared.Models.ADGroup.GetOrCreateAdminPermission(System.String)')
  - [GetOrCreatePermission(rolePermission)](#M-Authentication-Shared-Models-ADGroup-GetOrCreatePermission-Authentication-Shared-Models-CosmosRolePermission- 'Authentication.Shared.Models.ADGroup.GetOrCreatePermission(Authentication.Shared.Models.CosmosRolePermission)')
  - [GetPermissions()](#M-Authentication-Shared-Models-ADGroup-GetPermissions 'Authentication.Shared.Models.ADGroup.GetPermissions')
  - [HasUser(userId)](#M-Authentication-Shared-Models-ADGroup-HasUser-System-String- 'Authentication.Shared.Models.ADGroup.HasUser(System.String)')
  - [RemoveUser(userId)](#M-Authentication-Shared-Models-ADGroup-RemoveUser-System-String- 'Authentication.Shared.Models.ADGroup.RemoveUser(System.String)')
- [ADToken](#T-Authentication-Shared-Models-ADToken 'Authentication.Shared.Models.ADToken')
  - [AccessToken](#P-Authentication-Shared-Models-ADToken-AccessToken 'Authentication.Shared.Models.ADToken.AccessToken')
  - [ExpiresIn](#P-Authentication-Shared-Models-ADToken-ExpiresIn 'Authentication.Shared.Models.ADToken.ExpiresIn')
  - [ExpiresOn](#P-Authentication-Shared-Models-ADToken-ExpiresOn 'Authentication.Shared.Models.ADToken.ExpiresOn')
  - [IsExpired](#P-Authentication-Shared-Models-ADToken-IsExpired 'Authentication.Shared.Models.ADToken.IsExpired')
  - [NotBefore](#P-Authentication-Shared-Models-ADToken-NotBefore 'Authentication.Shared.Models.ADToken.NotBefore')
  - [RefreshToken](#P-Authentication-Shared-Models-ADToken-RefreshToken 'Authentication.Shared.Models.ADToken.RefreshToken')
  - [Resource](#P-Authentication-Shared-Models-ADToken-Resource 'Authentication.Shared.Models.ADToken.Resource')
- [ADUser](#T-Authentication-Shared-Models-ADUser 'Authentication.Shared.Models.ADUser')
  - [AccountEnabled](#P-Authentication-Shared-Models-ADUser-AccountEnabled 'Authentication.Shared.Models.ADUser.AccountEnabled')
  - [ObjectId](#P-Authentication-Shared-Models-ADUser-ObjectId 'Authentication.Shared.Models.ADUser.ObjectId')
  - [PasswordPolicies](#P-Authentication-Shared-Models-ADUser-PasswordPolicies 'Authentication.Shared.Models.ADUser.PasswordPolicies')
  - [SignInNames](#P-Authentication-Shared-Models-ADUser-SignInNames 'Authentication.Shared.Models.ADUser.SignInNames')
  - [UserType](#P-Authentication-Shared-Models-ADUser-UserType 'Authentication.Shared.Models.ADUser.UserType')
  - [FindByEmail(email)](#M-Authentication-Shared-Models-ADUser-FindByEmail-System-String- 'Authentication.Shared.Models.ADUser.FindByEmail(System.String)')
  - [FindById(id)](#M-Authentication-Shared-Models-ADUser-FindById-System-String- 'Authentication.Shared.Models.ADUser.FindById(System.String)')
  - [FindOrCreate(email,name)](#M-Authentication-Shared-Models-ADUser-FindOrCreate-System-String,System-String- 'Authentication.Shared.Models.ADUser.FindOrCreate(System.String,System.String)')
  - [GetOrCreateAdminPermissions(rolePermission)](#M-Authentication-Shared-Models-ADUser-GetOrCreateAdminPermissions-Authentication-Shared-Models-CosmosRolePermission- 'Authentication.Shared.Models.ADUser.GetOrCreateAdminPermissions(Authentication.Shared.Models.CosmosRolePermission)')
  - [GetOrCreateSharePermissions(connection)](#M-Authentication-Shared-Models-ADUser-GetOrCreateSharePermissions-Authentication-Shared-Models-Connection- 'Authentication.Shared.Models.ADUser.GetOrCreateSharePermissions(Authentication.Shared.Models.Connection)')
  - [GetOrCreateShareProfilePermissions(connection)](#M-Authentication-Shared-Models-ADUser-GetOrCreateShareProfilePermissions-Authentication-Shared-Models-Connection- 'Authentication.Shared.Models.ADUser.GetOrCreateShareProfilePermissions(Authentication.Shared.Models.Connection)')
  - [GetOrCreateUserPermissions(rolePermission)](#M-Authentication-Shared-Models-ADUser-GetOrCreateUserPermissions-Authentication-Shared-Models-CosmosRolePermission- 'Authentication.Shared.Models.ADUser.GetOrCreateUserPermissions(Authentication.Shared.Models.CosmosRolePermission)')
  - [GetPermissions()](#M-Authentication-Shared-Models-ADUser-GetPermissions-System-String- 'Authentication.Shared.Models.ADUser.GetPermissions(System.String)')
  - [GroupIds()](#M-Authentication-Shared-Models-ADUser-GroupIds 'Authentication.Shared.Models.ADUser.GroupIds')
  - [UpdateGroup(newGroupName)](#M-Authentication-Shared-Models-ADUser-UpdateGroup-System-String- 'Authentication.Shared.Models.ADUser.UpdateGroup(System.String)')
- [APIResult](#T-Authentication-Shared-Responses-APIResult 'Authentication.Shared.Responses.APIResult')
  - [Metadata](#P-Authentication-Shared-Responses-APIResult-Metadata 'Authentication.Shared.Responses.APIResult.Metadata')
  - [Value](#P-Authentication-Shared-Responses-APIResult-Value 'Authentication.Shared.Responses.APIResult.Value')
- [AddUserToGroupParameter](#T-Authentication-Shared-Requests-AddUserToGroupParameter 'Authentication.Shared.Requests.AddUserToGroupParameter')
  - [Url](#P-Authentication-Shared-Requests-AddUserToGroupParameter-Url 'Authentication.Shared.Requests.AddUserToGroupParameter.Url')
- [AutoGeneratedIAzureGraphRestApi](#T-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi')
  - [#ctor()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.#ctor(System.Net.Http.HttpClient,Refit.IRequestBuilder)')
  - [Client](#P-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Client 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Client')
  - [Authentication#Shared#Services#IAzureGraphRestApi#AddUserToGroup()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#AddUserToGroup-System-String,System-String,System-String,Authentication-Shared-Requests-AddUserToGroupParameter- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#AddUserToGroup(System.String,System.String,System.String,Authentication.Shared.Requests.AddUserToGroupParameter)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#CreateUser()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#CreateUser-System-String,System-String,Authentication-Shared-Requests-CreateADUserParameters- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#CreateUser(System.String,System.String,Authentication.Shared.Requests.CreateADUserParameters)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#GetAllGroups()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetAllGroups-System-String,System-String- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#GetAllGroups(System.String,System.String)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#GetUserById()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetUserById-System-String,System-String,System-String- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#GetUserById(System.String,System.String,System.String)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#GetUserGroups()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetUserGroups-System-String,System-String,System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#GetUserGroups(System.String,System.String,System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [Authentication#Shared#Services#IAzureGraphRestApi#IsMemberOf()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#IsMemberOf-System-String,System-String,Authentication-Shared-Requests-IsMemberOfParam- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#IsMemberOf(System.String,System.String,Authentication.Shared.Requests.IsMemberOfParam)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#RemoveUserFromGroup()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#RemoveUserFromGroup-System-String,System-String,System-String,System-String- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#RemoveUserFromGroup(System.String,System.String,System.String,System.String)')
  - [Authentication#Shared#Services#IAzureGraphRestApi#SearchUser()](#M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#SearchUser-System-String,System-String,System-String- 'Authentication.Shared.Services.AutoGeneratedIAzureGraphRestApi.Authentication#Shared#Services#IAzureGraphRestApi#SearchUser(System.String,System.String,System.String)')
- [AutoGeneratedIB2CRestApi](#T-Authentication-Shared-Services-AutoGeneratedIB2CRestApi 'Authentication.Shared.Services.AutoGeneratedIB2CRestApi')
  - [#ctor()](#M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder- 'Authentication.Shared.Services.AutoGeneratedIB2CRestApi.#ctor(System.Net.Http.HttpClient,Refit.IRequestBuilder)')
  - [Client](#P-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Client 'Authentication.Shared.Services.AutoGeneratedIB2CRestApi.Client')
  - [Authentication#Shared#Services#IB2CRestApi#GetAccessToken()](#M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Authentication#Shared#Services#IB2CRestApi#GetAccessToken-System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedIB2CRestApi.Authentication#Shared#Services#IB2CRestApi#GetAccessToken(System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [Authentication#Shared#Services#IB2CRestApi#RefreshToken()](#M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Authentication#Shared#Services#IB2CRestApi#RefreshToken-System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedIB2CRestApi.Authentication#Shared#Services#IB2CRestApi#RefreshToken(System.Collections.Generic.Dictionary{System.String,System.Object})')
- [AutoGeneratedMicrosoftServiceIMicrosoftRestApi](#T-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi')
  - [#ctor()](#M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder- 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi.#ctor(System.Net.Http.HttpClient,Refit.IRequestBuilder)')
  - [Client](#P-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Client 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi.Client')
  - [Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetAccessToken()](#M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetAccessToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi.Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetAccessToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetMasterToken()](#M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetMasterToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi.Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetMasterToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#RefreshToken()](#M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#RefreshToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.AutoGeneratedMicrosoftServiceIMicrosoftRestApi.Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#RefreshToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
- [AzureB2C](#T-Authentication-Shared-Configurations-AzureB2C 'Authentication.Shared.Configurations.AzureB2C')
  - [AdminClientId](#F-Authentication-Shared-Configurations-AzureB2C-AdminClientId 'Authentication.Shared.Configurations.AzureB2C.AdminClientId')
  - [AdminClientSecret](#F-Authentication-Shared-Configurations-AzureB2C-AdminClientSecret 'Authentication.Shared.Configurations.AzureB2C.AdminClientSecret')
  - [AdminGroup](#F-Authentication-Shared-Configurations-AzureB2C-AdminGroup 'Authentication.Shared.Configurations.AzureB2C.AdminGroup')
  - [AuthPolicy](#F-Authentication-Shared-Configurations-AzureB2C-AuthPolicy 'Authentication.Shared.Configurations.AzureB2C.AuthPolicy')
  - [B2CClientId](#F-Authentication-Shared-Configurations-AzureB2C-B2CClientId 'Authentication.Shared.Configurations.AzureB2C.B2CClientId')
  - [B2CUrl](#F-Authentication-Shared-Configurations-AzureB2C-B2CUrl 'Authentication.Shared.Configurations.AzureB2C.B2CUrl')
  - [BearerAuthentication](#F-Authentication-Shared-Configurations-AzureB2C-BearerAuthentication 'Authentication.Shared.Configurations.AzureB2C.BearerAuthentication')
  - [GrantTypeCredentials](#F-Authentication-Shared-Configurations-AzureB2C-GrantTypeCredentials 'Authentication.Shared.Configurations.AzureB2C.GrantTypeCredentials')
  - [GrantTypePassword](#F-Authentication-Shared-Configurations-AzureB2C-GrantTypePassword 'Authentication.Shared.Configurations.AzureB2C.GrantTypePassword')
  - [GrantTypeRefreshToken](#F-Authentication-Shared-Configurations-AzureB2C-GrantTypeRefreshToken 'Authentication.Shared.Configurations.AzureB2C.GrantTypeRefreshToken')
  - [GraphResource](#F-Authentication-Shared-Configurations-AzureB2C-GraphResource 'Authentication.Shared.Configurations.AzureB2C.GraphResource')
  - [GuestGroup](#F-Authentication-Shared-Configurations-AzureB2C-GuestGroup 'Authentication.Shared.Configurations.AzureB2C.GuestGroup')
  - [IdTokenType](#F-Authentication-Shared-Configurations-AzureB2C-IdTokenType 'Authentication.Shared.Configurations.AzureB2C.IdTokenType')
  - [MicorsoftAuthUrl](#F-Authentication-Shared-Configurations-AzureB2C-MicorsoftAuthUrl 'Authentication.Shared.Configurations.AzureB2C.MicorsoftAuthUrl')
  - [NewGroup](#F-Authentication-Shared-Configurations-AzureB2C-NewGroup 'Authentication.Shared.Configurations.AzureB2C.NewGroup')
  - [PasswordPrefix](#F-Authentication-Shared-Configurations-AzureB2C-PasswordPrefix 'Authentication.Shared.Configurations.AzureB2C.PasswordPrefix')
  - [PasswordSecretKey](#F-Authentication-Shared-Configurations-AzureB2C-PasswordSecretKey 'Authentication.Shared.Configurations.AzureB2C.PasswordSecretKey')
  - [SignInSignUpPolicy](#F-Authentication-Shared-Configurations-AzureB2C-SignInSignUpPolicy 'Authentication.Shared.Configurations.AzureB2C.SignInSignUpPolicy')
  - [TenantId](#F-Authentication-Shared-Configurations-AzureB2C-TenantId 'Authentication.Shared.Configurations.AzureB2C.TenantId')
  - [TenantName](#F-Authentication-Shared-Configurations-AzureB2C-TenantName 'Authentication.Shared.Configurations.AzureB2C.TenantName')
  - [TokenType](#F-Authentication-Shared-Configurations-AzureB2C-TokenType 'Authentication.Shared.Configurations.AzureB2C.TokenType')
- [AzureB2CService](#T-Authentication-Shared-Services-AzureB2CService 'Authentication.Shared.Services.AzureB2CService')
  - [#ctor()](#M-Authentication-Shared-Services-AzureB2CService-#ctor 'Authentication.Shared.Services.AzureB2CService.#ctor')
  - [azureGraphRestApi](#F-Authentication-Shared-Services-AzureB2CService-azureGraphRestApi 'Authentication.Shared.Services.AzureB2CService.azureGraphRestApi')
  - [b2cHttpClient](#F-Authentication-Shared-Services-AzureB2CService-b2cHttpClient 'Authentication.Shared.Services.AzureB2CService.b2cHttpClient')
  - [b2cRestApi](#F-Authentication-Shared-Services-AzureB2CService-b2cRestApi 'Authentication.Shared.Services.AzureB2CService.b2cRestApi')
  - [graphHttpClient](#F-Authentication-Shared-Services-AzureB2CService-graphHttpClient 'Authentication.Shared.Services.AzureB2CService.graphHttpClient')
  - [Instance](#P-Authentication-Shared-Services-AzureB2CService-Instance 'Authentication.Shared.Services.AzureB2CService.Instance')
  - [AddUserToGroup(groupId,userId)](#M-Authentication-Shared-Services-AzureB2CService-AddUserToGroup-System-String,System-String- 'Authentication.Shared.Services.AzureB2CService.AddUserToGroup(System.String,System.String)')
  - [CreateADUser(parameters)](#M-Authentication-Shared-Services-AzureB2CService-CreateADUser-Authentication-Shared-Requests-CreateADUserParameters- 'Authentication.Shared.Services.AzureB2CService.CreateADUser(Authentication.Shared.Requests.CreateADUserParameters)')
  - [GetADUserByEmail(email)](#M-Authentication-Shared-Services-AzureB2CService-GetADUserByEmail-System-String- 'Authentication.Shared.Services.AzureB2CService.GetADUserByEmail(System.String)')
  - [GetAllGroups()](#M-Authentication-Shared-Services-AzureB2CService-GetAllGroups 'Authentication.Shared.Services.AzureB2CService.GetAllGroups')
  - [GetB2CAccessToken(email,password)](#M-Authentication-Shared-Services-AzureB2CService-GetB2CAccessToken-System-String,System-String- 'Authentication.Shared.Services.AzureB2CService.GetB2CAccessToken(System.String,System.String)')
  - [GetUserById()))](#M-Authentication-Shared-Services-AzureB2CService-GetUserById-System-String- 'Authentication.Shared.Services.AzureB2CService.GetUserById(System.String)')
  - [GetUserGroups(userId)](#M-Authentication-Shared-Services-AzureB2CService-GetUserGroups-System-String- 'Authentication.Shared.Services.AzureB2CService.GetUserGroups(System.String)')
  - [IsMemberOfGroup(groupId,userId)](#M-Authentication-Shared-Services-AzureB2CService-IsMemberOfGroup-System-String,System-String- 'Authentication.Shared.Services.AzureB2CService.IsMemberOfGroup(System.String,System.String)')
  - [RefreshB2CToken(refreshToken)](#M-Authentication-Shared-Services-AzureB2CService-RefreshB2CToken-System-String- 'Authentication.Shared.Services.AzureB2CService.RefreshB2CToken(System.String)')
  - [RemoveUserFromGroup(groupId,userId)](#M-Authentication-Shared-Services-AzureB2CService-RemoveUserFromGroup-System-String,System-String- 'Authentication.Shared.Services.AzureB2CService.RemoveUserFromGroup(System.String,System.String)')
- [CheckAccount](#T-Authentication-CheckAccount 'Authentication.CheckAccount')
  - [Run(req,log)](#M-Authentication-CheckAccount-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.CheckAccount.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [Configurations](#T-Authentication-Shared-Configurations 'Authentication.Shared.Configurations')
  - [Configuration](#P-Authentication-Shared-Configurations-Configuration 'Authentication.Shared.Configurations.Configuration')
- [Connection](#T-Authentication-Shared-Models-Connection 'Authentication.Shared.Models.Connection')
  - [CreatedAt](#P-Authentication-Shared-Models-Connection-CreatedAt 'Authentication.Shared.Models.Connection.CreatedAt')
  - [Id](#P-Authentication-Shared-Models-Connection-Id 'Authentication.Shared.Models.Connection.Id')
  - [IsReadOnly](#P-Authentication-Shared-Models-Connection-IsReadOnly 'Authentication.Shared.Models.Connection.IsReadOnly')
  - [Partition](#P-Authentication-Shared-Models-Connection-Partition 'Authentication.Shared.Models.Connection.Partition')
  - [Permission](#P-Authentication-Shared-Models-Connection-Permission 'Authentication.Shared.Models.Connection.Permission')
  - [Profiles](#P-Authentication-Shared-Models-Connection-Profiles 'Authentication.Shared.Models.Connection.Profiles')
  - [Status](#P-Authentication-Shared-Models-Connection-Status 'Authentication.Shared.Models.Connection.Status')
  - [Table](#P-Authentication-Shared-Models-Connection-Table 'Authentication.Shared.Models.Connection.Table')
  - [UpdatedAt](#P-Authentication-Shared-Models-Connection-UpdatedAt 'Authentication.Shared.Models.Connection.UpdatedAt')
  - [User1](#P-Authentication-Shared-Models-Connection-User1 'Authentication.Shared.Models.Connection.User1')
  - [User2](#P-Authentication-Shared-Models-Connection-User2 'Authentication.Shared.Models.Connection.User2')
  - [CreateOrUpdate()](#M-Authentication-Shared-Models-Connection-CreateOrUpdate 'Authentication.Shared.Models.Connection.CreateOrUpdate')
  - [CreatePermission()](#M-Authentication-Shared-Models-Connection-CreatePermission 'Authentication.Shared.Models.Connection.CreatePermission')
  - [CreateProfilePermission()](#M-Authentication-Shared-Models-Connection-CreateProfilePermission 'Authentication.Shared.Models.Connection.CreateProfilePermission')
  - [GetPermission()](#M-Authentication-Shared-Models-Connection-GetPermission 'Authentication.Shared.Models.Connection.GetPermission')
  - [GetProfilePermission()](#M-Authentication-Shared-Models-Connection-GetProfilePermission 'Authentication.Shared.Models.Connection.GetProfilePermission')
  - [QueryByShareUser(userId)](#M-Authentication-Shared-Models-Connection-QueryByShareUser-System-String- 'Authentication.Shared.Models.Connection.QueryByShareUser(System.String)')
  - [UpdatePermission()](#M-Authentication-Shared-Models-Connection-UpdatePermission 'Authentication.Shared.Models.Connection.UpdatePermission')
  - [UpdateProfilePermission()](#M-Authentication-Shared-Models-Connection-UpdateProfilePermission 'Authentication.Shared.Models.Connection.UpdateProfilePermission')
- [ConnectionToken](#T-Authentication-Shared-Models-ConnectionToken 'Authentication.Shared.Models.ConnectionToken')
  - [ChildFirstName](#P-Authentication-Shared-Models-ConnectionToken-ChildFirstName 'Authentication.Shared.Models.ConnectionToken.ChildFirstName')
  - [ChildLastName](#P-Authentication-Shared-Models-ConnectionToken-ChildLastName 'Authentication.Shared.Models.ConnectionToken.ChildLastName')
  - [CreatedAt](#P-Authentication-Shared-Models-ConnectionToken-CreatedAt 'Authentication.Shared.Models.ConnectionToken.CreatedAt')
  - [Email](#P-Authentication-Shared-Models-ConnectionToken-Email 'Authentication.Shared.Models.ConnectionToken.Email')
  - [FirstName](#P-Authentication-Shared-Models-ConnectionToken-FirstName 'Authentication.Shared.Models.ConnectionToken.FirstName')
  - [FromEmail](#P-Authentication-Shared-Models-ConnectionToken-FromEmail 'Authentication.Shared.Models.ConnectionToken.FromEmail')
  - [FromFirstName](#P-Authentication-Shared-Models-ConnectionToken-FromFirstName 'Authentication.Shared.Models.ConnectionToken.FromFirstName')
  - [FromId](#P-Authentication-Shared-Models-ConnectionToken-FromId 'Authentication.Shared.Models.ConnectionToken.FromId')
  - [FromLastName](#P-Authentication-Shared-Models-ConnectionToken-FromLastName 'Authentication.Shared.Models.ConnectionToken.FromLastName')
  - [Id](#P-Authentication-Shared-Models-ConnectionToken-Id 'Authentication.Shared.Models.ConnectionToken.Id')
  - [IsFromParent](#P-Authentication-Shared-Models-ConnectionToken-IsFromParent 'Authentication.Shared.Models.ConnectionToken.IsFromParent')
  - [LastName](#P-Authentication-Shared-Models-ConnectionToken-LastName 'Authentication.Shared.Models.ConnectionToken.LastName')
  - [Partition](#P-Authentication-Shared-Models-ConnectionToken-Partition 'Authentication.Shared.Models.ConnectionToken.Partition')
  - [Permission](#P-Authentication-Shared-Models-ConnectionToken-Permission 'Authentication.Shared.Models.ConnectionToken.Permission')
  - [State](#P-Authentication-Shared-Models-ConnectionToken-State 'Authentication.Shared.Models.ConnectionToken.State')
  - [ToId](#P-Authentication-Shared-Models-ConnectionToken-ToId 'Authentication.Shared.Models.ConnectionToken.ToId')
  - [Token](#P-Authentication-Shared-Models-ConnectionToken-Token 'Authentication.Shared.Models.ConnectionToken.Token')
  - [Type](#P-Authentication-Shared-Models-ConnectionToken-Type 'Authentication.Shared.Models.ConnectionToken.Type')
  - [UpdatedAt](#P-Authentication-Shared-Models-ConnectionToken-UpdatedAt 'Authentication.Shared.Models.ConnectionToken.UpdatedAt')
  - [Viewed](#P-Authentication-Shared-Models-ConnectionToken-Viewed 'Authentication.Shared.Models.ConnectionToken.Viewed')
  - [CreateOrUpdate()](#M-Authentication-Shared-Models-ConnectionToken-CreateOrUpdate 'Authentication.Shared.Models.ConnectionToken.CreateOrUpdate')
  - [GetById(id)](#M-Authentication-Shared-Models-ConnectionToken-GetById-System-String- 'Authentication.Shared.Models.ConnectionToken.GetById(System.String)')
  - [ParentAccepted(professionalUser)](#M-Authentication-Shared-Models-ConnectionToken-ParentAccepted-Authentication-Shared-Models-User- 'Authentication.Shared.Models.ConnectionToken.ParentAccepted(Authentication.Shared.Models.User)')
  - [ParentDeny(professionalUser)](#M-Authentication-Shared-Models-ConnectionToken-ParentDeny-Authentication-Shared-Models-User- 'Authentication.Shared.Models.ConnectionToken.ParentDeny(Authentication.Shared.Models.User)')
  - [ParentProcess()](#M-Authentication-Shared-Models-ConnectionToken-ParentProcess 'Authentication.Shared.Models.ConnectionToken.ParentProcess')
  - [ProfessionalInvite(parentUser)](#M-Authentication-Shared-Models-ConnectionToken-ProfessionalInvite-Authentication-Shared-Models-User- 'Authentication.Shared.Models.ConnectionToken.ProfessionalInvite(Authentication.Shared.Models.User)')
  - [ProfessionalProcess()](#M-Authentication-Shared-Models-ConnectionToken-ProfessionalProcess 'Authentication.Shared.Models.ConnectionToken.ProfessionalProcess')
  - [ProfessionalUnshare(parentUser)](#M-Authentication-Shared-Models-ConnectionToken-ProfessionalUnshare-Authentication-Shared-Models-User- 'Authentication.Shared.Models.ConnectionToken.ProfessionalUnshare(Authentication.Shared.Models.User)')
- [Cosmos](#T-Authentication-Shared-Configurations-Cosmos 'Authentication.Shared.Configurations.Cosmos')
  - [AcceptedPermissions](#F-Authentication-Shared-Configurations-Cosmos-AcceptedPermissions 'Authentication.Shared.Configurations.Cosmos.AcceptedPermissions')
  - [DatabaseId](#F-Authentication-Shared-Configurations-Cosmos-DatabaseId 'Authentication.Shared.Configurations.Cosmos.DatabaseId')
  - [DatabaseMasterKey](#F-Authentication-Shared-Configurations-Cosmos-DatabaseMasterKey 'Authentication.Shared.Configurations.Cosmos.DatabaseMasterKey')
  - [DatabaseUrl](#F-Authentication-Shared-Configurations-Cosmos-DatabaseUrl 'Authentication.Shared.Configurations.Cosmos.DatabaseUrl')
  - [DefaultPartition](#F-Authentication-Shared-Configurations-Cosmos-DefaultPartition 'Authentication.Shared.Configurations.Cosmos.DefaultPartition')
  - [PartitionKey](#F-Authentication-Shared-Configurations-Cosmos-PartitionKey 'Authentication.Shared.Configurations.Cosmos.PartitionKey')
  - [ResourceTokenExpiration](#F-Authentication-Shared-Configurations-Cosmos-ResourceTokenExpiration 'Authentication.Shared.Configurations.Cosmos.ResourceTokenExpiration')
- [CosmosRolePermission](#T-Authentication-Shared-Models-CosmosRolePermission 'Authentication.Shared.Models.CosmosRolePermission')
  - [Id](#P-Authentication-Shared-Models-CosmosRolePermission-Id 'Authentication.Shared.Models.CosmosRolePermission.Id')
  - [Permission](#P-Authentication-Shared-Models-CosmosRolePermission-Permission 'Authentication.Shared.Models.CosmosRolePermission.Permission')
  - [Role](#P-Authentication-Shared-Models-CosmosRolePermission-Role 'Authentication.Shared.Models.CosmosRolePermission.Role')
  - [Table](#P-Authentication-Shared-Models-CosmosRolePermission-Table 'Authentication.Shared.Models.CosmosRolePermission.Table')
  - [CreateCosmosPermission(userId,permissionId,partition)](#M-Authentication-Shared-Models-CosmosRolePermission-CreateCosmosPermission-System-String,System-String,System-String- 'Authentication.Shared.Models.CosmosRolePermission.CreateCosmosPermission(System.String,System.String,System.String)')
  - [CreateCosmosUser(userId)](#M-Authentication-Shared-Models-CosmosRolePermission-CreateCosmosUser-System-String- 'Authentication.Shared.Models.CosmosRolePermission.CreateCosmosUser(System.String)')
  - [GetAllTables()](#M-Authentication-Shared-Models-CosmosRolePermission-GetAllTables 'Authentication.Shared.Models.CosmosRolePermission.GetAllTables')
  - [QueryByRole(role)](#M-Authentication-Shared-Models-CosmosRolePermission-QueryByRole-System-String- 'Authentication.Shared.Models.CosmosRolePermission.QueryByRole(System.String)')
  - [QueryByTable(table)](#M-Authentication-Shared-Models-CosmosRolePermission-QueryByTable-System-String- 'Authentication.Shared.Models.CosmosRolePermission.QueryByTable(System.String)')
- [CreateADUserParameters](#T-Authentication-Shared-Requests-CreateADUserParameters 'Authentication.Shared.Requests.CreateADUserParameters')
  - [AccountEnable](#P-Authentication-Shared-Requests-CreateADUserParameters-AccountEnable 'Authentication.Shared.Requests.CreateADUserParameters.AccountEnable')
  - [CreationType](#P-Authentication-Shared-Requests-CreateADUserParameters-CreationType 'Authentication.Shared.Requests.CreateADUserParameters.CreationType')
  - [DisplayName](#P-Authentication-Shared-Requests-CreateADUserParameters-DisplayName 'Authentication.Shared.Requests.CreateADUserParameters.DisplayName')
  - [PasswordPolicies](#P-Authentication-Shared-Requests-CreateADUserParameters-PasswordPolicies 'Authentication.Shared.Requests.CreateADUserParameters.PasswordPolicies')
  - [Profile](#P-Authentication-Shared-Requests-CreateADUserParameters-Profile 'Authentication.Shared.Requests.CreateADUserParameters.Profile')
  - [SignInNames](#P-Authentication-Shared-Requests-CreateADUserParameters-SignInNames 'Authentication.Shared.Requests.CreateADUserParameters.SignInNames')
- [CreateRolePermission](#T-Authentication-CreateRolePermission 'Authentication.CreateRolePermission')
  - [CreateRolePermissionForRoleAsync(role)](#M-Authentication-CreateRolePermission-CreateRolePermissionForRoleAsync-System-String- 'Authentication.CreateRolePermission.CreateRolePermissionForRoleAsync(System.String)')
  - [CreateRolePermissionForTableAsync(table)](#M-Authentication-CreateRolePermission-CreateRolePermissionForTableAsync-System-String- 'Authentication.CreateRolePermission.CreateRolePermissionForTableAsync(System.String)')
  - [Run(req,log)](#M-Authentication-CreateRolePermission-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.CreateRolePermission.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [DataService](#T-Authentication-Shared-Services-DataService 'Authentication.Shared.Services.DataService')
  - [#ctor()](#M-Authentication-Shared-Services-DataService-#ctor 'Authentication.Shared.Services.DataService.#ctor')
  - [client](#F-Authentication-Shared-Services-DataService-client 'Authentication.Shared.Services.DataService.client')
  - [Instance](#P-Authentication-Shared-Services-DataService-Instance 'Authentication.Shared.Services.DataService.Instance')
  - [ClearAllAsync()](#M-Authentication-Shared-Services-DataService-ClearAllAsync 'Authentication.Shared.Services.DataService.ClearAllAsync')
  - [CreatePermission(userId,permissionId,readOnly,tableName,partition)](#M-Authentication-Shared-Services-DataService-CreatePermission-System-String,System-String,System-Boolean,System-String,System-String- 'Authentication.Shared.Services.DataService.CreatePermission(System.String,System.String,System.Boolean,System.String,System.String)')
  - [CreateUser(userId)](#M-Authentication-Shared-Services-DataService-CreateUser-System-String- 'Authentication.Shared.Services.DataService.CreateUser(System.String)')
  - [GetAllTables()](#M-Authentication-Shared-Services-DataService-GetAllTables 'Authentication.Shared.Services.DataService.GetAllTables')
  - [GetPermission(userId,permissionName)](#M-Authentication-Shared-Services-DataService-GetPermission-System-String,System-String- 'Authentication.Shared.Services.DataService.GetPermission(System.String,System.String)')
  - [GetPermissions(userId)](#M-Authentication-Shared-Services-DataService-GetPermissions-System-String- 'Authentication.Shared.Services.DataService.GetPermissions(System.String)')
  - [ListUsers()](#M-Authentication-Shared-Services-DataService-ListUsers 'Authentication.Shared.Services.DataService.ListUsers')
  - [QueryDocuments\`\`1(collectionName,query,partition,crossPartition)](#M-Authentication-Shared-Services-DataService-QueryDocuments``1-System-String,Microsoft-Azure-Cosmos-QueryDefinition,System-String,System-Boolean- 'Authentication.Shared.Services.DataService.QueryDocuments``1(System.String,Microsoft.Azure.Cosmos.QueryDefinition,System.String,System.Boolean)')
  - [RemovePermission(userId,permissionName)](#M-Authentication-Shared-Services-DataService-RemovePermission-System-String,System-String- 'Authentication.Shared.Services.DataService.RemovePermission(System.String,System.String)')
  - [ReplacePermission(userId,permissionId,readOnly,tableName,partition)](#M-Authentication-Shared-Services-DataService-ReplacePermission-System-String,System-String,System-Boolean,System-String,System-String- 'Authentication.Shared.Services.DataService.ReplacePermission(System.String,System.String,System.Boolean,System.String,System.String)')
- [Dictionary](#T-Extensions-Dictionary 'Extensions.Dictionary')
  - [AddIfNotEmpty\`\`1(dictionary,key,value)](#M-Extensions-Dictionary-AddIfNotEmpty``1-System-Collections-Generic-Dictionary{``0,System-Object},``0,System-Object- 'Extensions.Dictionary.AddIfNotEmpty``1(System.Collections.Generic.Dictionary{``0,System.Object},``0,System.Object)')
- [GetAccessToken](#T-Authentication-GetAccessToken 'Authentication.GetAccessToken')
  - [GetAccessTokenFromIdToken(idToken)](#M-Authentication-GetAccessToken-GetAccessTokenFromIdToken-System-String- 'Authentication.GetAccessToken.GetAccessTokenFromIdToken(System.String)')
  - [GetAccessTokenFromLogin(email,password)](#M-Authentication-GetAccessToken-GetAccessTokenFromLogin-System-String,System-String- 'Authentication.GetAccessToken.GetAccessTokenFromLogin(System.String,System.String)')
  - [Run(req,log)](#M-Authentication-GetAccessToken-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.GetAccessToken.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [GetResourceTokens](#T-Authentication-GetResourceTokens 'Authentication.GetResourceTokens')
  - [Run(req,log)](#M-Authentication-GetResourceTokens-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.GetResourceTokens.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [GetUserInfo](#T-Authentication-GetUserInfo 'Authentication.GetUserInfo')
  - [Run(req,log)](#M-Authentication-GetUserInfo-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.GetUserInfo.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [GroupsResponse](#T-Authentication-Shared-Responses-GroupsResponse 'Authentication.Shared.Responses.GroupsResponse')
  - [Groups](#P-Authentication-Shared-Responses-GroupsResponse-Groups 'Authentication.Shared.Responses.GroupsResponse.Groups')
  - [Metadata](#P-Authentication-Shared-Responses-GroupsResponse-Metadata 'Authentication.Shared.Responses.GroupsResponse.Metadata')
- [HttpHelper](#T-Authentication-Shared-Utils-HttpHelper 'Authentication.Shared.Utils.HttpHelper')
  - [CreateErrorResponse(message,statusCode)](#M-Authentication-Shared-Utils-HttpHelper-CreateErrorResponse-System-String,System-Int32- 'Authentication.Shared.Utils.HttpHelper.CreateErrorResponse(System.String,System.Int32)')
  - [CreateSuccessResponse()](#M-Authentication-Shared-Utils-HttpHelper-CreateSuccessResponse 'Authentication.Shared.Utils.HttpHelper.CreateSuccessResponse')
  - [GeneratePassword(email)](#M-Authentication-Shared-Utils-HttpHelper-GeneratePassword-System-String- 'Authentication.Shared.Utils.HttpHelper.GeneratePassword(System.String)')
  - [GetBearerAuthorization(token)](#M-Authentication-Shared-Utils-HttpHelper-GetBearerAuthorization-System-String- 'Authentication.Shared.Utils.HttpHelper.GetBearerAuthorization(System.String)')
  - [GetIpFromRequestHeaders(request)](#M-Authentication-Shared-Utils-HttpHelper-GetIpFromRequestHeaders-Microsoft-AspNetCore-Http-HttpRequest- 'Authentication.Shared.Utils.HttpHelper.GetIpFromRequestHeaders(Microsoft.AspNetCore.Http.HttpRequest)')
  - [VerifyAdminToken(authToken)](#M-Authentication-Shared-Utils-HttpHelper-VerifyAdminToken-System-String- 'Authentication.Shared.Utils.HttpHelper.VerifyAdminToken(System.String)')
- [HttpLoggingHandler](#T-Authentication-Shared-Utils-HttpLoggingHandler 'Authentication.Shared.Utils.HttpLoggingHandler')
  - [#ctor(innerHandler)](#M-Authentication-Shared-Utils-HttpLoggingHandler-#ctor-System-Net-Http-HttpMessageHandler- 'Authentication.Shared.Utils.HttpLoggingHandler.#ctor(System.Net.Http.HttpMessageHandler)')
  - [types](#F-Authentication-Shared-Utils-HttpLoggingHandler-types 'Authentication.Shared.Utils.HttpLoggingHandler.types')
  - [IsTextBasedContentType(headers)](#M-Authentication-Shared-Utils-HttpLoggingHandler-IsTextBasedContentType-System-Net-Http-Headers-HttpHeaders- 'Authentication.Shared.Utils.HttpLoggingHandler.IsTextBasedContentType(System.Net.Http.Headers.HttpHeaders)')
  - [SendAsync(request,cancellationToken)](#M-Authentication-Shared-Utils-HttpLoggingHandler-SendAsync-System-Net-Http-HttpRequestMessage,System-Threading-CancellationToken- 'Authentication.Shared.Utils.HttpLoggingHandler.SendAsync(System.Net.Http.HttpRequestMessage,System.Threading.CancellationToken)')
- [IAzureGraphRestApi](#T-Authentication-Shared-Services-IAzureGraphRestApi 'Authentication.Shared.Services.IAzureGraphRestApi')
  - [AddUserToGroup(tenantId,groupId,accessToken,parameters)](#M-Authentication-Shared-Services-IAzureGraphRestApi-AddUserToGroup-System-String,System-String,System-String,Authentication-Shared-Requests-AddUserToGroupParameter- 'Authentication.Shared.Services.IAzureGraphRestApi.AddUserToGroup(System.String,System.String,System.String,Authentication.Shared.Requests.AddUserToGroupParameter)')
  - [CreateUser(tenantId,accessToken,param)](#M-Authentication-Shared-Services-IAzureGraphRestApi-CreateUser-System-String,System-String,Authentication-Shared-Requests-CreateADUserParameters- 'Authentication.Shared.Services.IAzureGraphRestApi.CreateUser(System.String,System.String,Authentication.Shared.Requests.CreateADUserParameters)')
  - [GetAllGroups(tenantId,accessToken)](#M-Authentication-Shared-Services-IAzureGraphRestApi-GetAllGroups-System-String,System-String- 'Authentication.Shared.Services.IAzureGraphRestApi.GetAllGroups(System.String,System.String)')
  - [GetUserById(tenantId,userId,accessToken)](#M-Authentication-Shared-Services-IAzureGraphRestApi-GetUserById-System-String,System-String,System-String- 'Authentication.Shared.Services.IAzureGraphRestApi.GetUserById(System.String,System.String,System.String)')
  - [GetUserGroups(tenantId,userId,accessToken,parameters)](#M-Authentication-Shared-Services-IAzureGraphRestApi-GetUserGroups-System-String,System-String,System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.IAzureGraphRestApi.GetUserGroups(System.String,System.String,System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [IsMemberOf(tenantId,accessToken,param)](#M-Authentication-Shared-Services-IAzureGraphRestApi-IsMemberOf-System-String,System-String,Authentication-Shared-Requests-IsMemberOfParam- 'Authentication.Shared.Services.IAzureGraphRestApi.IsMemberOf(System.String,System.String,Authentication.Shared.Requests.IsMemberOfParam)')
  - [RemoveUserFromGroup(tenantId,userId,groupId,accessToken)](#M-Authentication-Shared-Services-IAzureGraphRestApi-RemoveUserFromGroup-System-String,System-String,System-String,System-String- 'Authentication.Shared.Services.IAzureGraphRestApi.RemoveUserFromGroup(System.String,System.String,System.String,System.String)')
  - [SearchUser(tenantId,accessToken,query)](#M-Authentication-Shared-Services-IAzureGraphRestApi-SearchUser-System-String,System-String,System-String- 'Authentication.Shared.Services.IAzureGraphRestApi.SearchUser(System.String,System.String,System.String)')
- [IB2CRestApi](#T-Authentication-Shared-Services-IB2CRestApi 'Authentication.Shared.Services.IB2CRestApi')
  - [GetAccessToken(parameters)](#M-Authentication-Shared-Services-IB2CRestApi-GetAccessToken-System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.IB2CRestApi.GetAccessToken(System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [RefreshToken(parameters)](#M-Authentication-Shared-Services-IB2CRestApi-RefreshToken-System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.IB2CRestApi.RefreshToken(System.Collections.Generic.Dictionary{System.String,System.Object})')
- [IMicrosoftRestApi](#T-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi 'Authentication.Shared.Services.MicrosoftService.IMicrosoftRestApi')
  - [GetAccessToken(tenantId,data)](#M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-GetAccessToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.MicrosoftService.IMicrosoftRestApi.GetAccessToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [GetMasterToken(tenantId,data)](#M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-GetMasterToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.MicrosoftService.IMicrosoftRestApi.GetMasterToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
  - [RefreshToken(tenantId,data)](#M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-RefreshToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}- 'Authentication.Shared.Services.MicrosoftService.IMicrosoftRestApi.RefreshToken(System.String,System.Collections.Generic.Dictionary{System.String,System.Object})')
- [IsMemberOfParam](#T-Authentication-Shared-Requests-IsMemberOfParam 'Authentication.Shared.Requests.IsMemberOfParam')
  - [GroupId](#P-Authentication-Shared-Requests-IsMemberOfParam-GroupId 'Authentication.Shared.Requests.IsMemberOfParam.GroupId')
  - [MemeberId](#P-Authentication-Shared-Requests-IsMemberOfParam-MemeberId 'Authentication.Shared.Requests.IsMemberOfParam.MemeberId')
- [JWTToken](#T-Authentication-Shared-Configurations-JWTToken 'Authentication.Shared.Configurations.JWTToken')
  - [TokenClientSecret](#F-Authentication-Shared-Configurations-JWTToken-TokenClientSecret 'Authentication.Shared.Configurations.JWTToken.TokenClientSecret')
  - [TokenIssuer](#F-Authentication-Shared-Configurations-JWTToken-TokenIssuer 'Authentication.Shared.Configurations.JWTToken.TokenIssuer')
  - [TokenSubject](#F-Authentication-Shared-Configurations-JWTToken-TokenSubject 'Authentication.Shared.Configurations.JWTToken.TokenSubject')
- [Logger](#T-Authentication-Shared-Utils-Logger 'Authentication.Shared.Utils.Logger')
  - [Log](#P-Authentication-Shared-Utils-Logger-Log 'Authentication.Shared.Utils.Logger.Log')
- [MicrosoftService](#T-Authentication-Shared-Services-MicrosoftService 'Authentication.Shared.Services.MicrosoftService')
  - [#ctor()](#M-Authentication-Shared-Services-MicrosoftService-#ctor 'Authentication.Shared.Services.MicrosoftService.#ctor')
  - [httpClient](#F-Authentication-Shared-Services-MicrosoftService-httpClient 'Authentication.Shared.Services.MicrosoftService.httpClient')
  - [service](#F-Authentication-Shared-Services-MicrosoftService-service 'Authentication.Shared.Services.MicrosoftService.service')
  - [Instance](#P-Authentication-Shared-Services-MicrosoftService-Instance 'Authentication.Shared.Services.MicrosoftService.Instance')
  - [GetAdminAccessToken(email,password)](#M-Authentication-Shared-Services-MicrosoftService-GetAdminAccessToken-System-String,System-String- 'Authentication.Shared.Services.MicrosoftService.GetAdminAccessToken(System.String,System.String)')
  - [GetMasterToken()](#M-Authentication-Shared-Services-MicrosoftService-GetMasterToken 'Authentication.Shared.Services.MicrosoftService.GetMasterToken')
  - [RefreshAdminAccessToken(refreshToken)](#M-Authentication-Shared-Services-MicrosoftService-RefreshAdminAccessToken-System-String- 'Authentication.Shared.Services.MicrosoftService.RefreshAdminAccessToken(System.String)')
- [PasswordProfile](#T-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile 'Authentication.Shared.Requests.CreateADUserParameters.PasswordProfile')
  - [ForceChangePasswordNextLogin](#P-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile-ForceChangePasswordNextLogin 'Authentication.Shared.Requests.CreateADUserParameters.PasswordProfile.ForceChangePasswordNextLogin')
  - [Password](#P-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile-Password 'Authentication.Shared.Requests.CreateADUserParameters.PasswordProfile.Password')
- [Profile](#T-Authentication-Shared-Models-Profile 'Authentication.Shared.Models.Profile')
  - [DateOfBirth](#P-Authentication-Shared-Models-Profile-DateOfBirth 'Authentication.Shared.Models.Profile.DateOfBirth')
  - [FirstName](#P-Authentication-Shared-Models-Profile-FirstName 'Authentication.Shared.Models.Profile.FirstName')
  - [Gender](#P-Authentication-Shared-Models-Profile-Gender 'Authentication.Shared.Models.Profile.Gender')
  - [Id](#P-Authentication-Shared-Models-Profile-Id 'Authentication.Shared.Models.Profile.Id')
  - [LastName](#P-Authentication-Shared-Models-Profile-LastName 'Authentication.Shared.Models.Profile.LastName')
  - [Partition](#P-Authentication-Shared-Models-Profile-Partition 'Authentication.Shared.Models.Profile.Partition')
  - [RecommendProgram](#P-Authentication-Shared-Models-Profile-RecommendProgram 'Authentication.Shared.Models.Profile.RecommendProgram')
  - [UserId](#P-Authentication-Shared-Models-Profile-UserId 'Authentication.Shared.Models.Profile.UserId')
  - [GetById(userId,profileId)](#M-Authentication-Shared-Models-Profile-GetById-System-String,System-String- 'Authentication.Shared.Models.Profile.GetById(System.String,System.String)')
  - [GetByUserId(userId)](#M-Authentication-Shared-Models-Profile-GetByUserId-System-String- 'Authentication.Shared.Models.Profile.GetByUserId(System.String)')
- [RefreshToken](#T-Authentication-RefreshToken 'Authentication.RefreshToken')
  - [Run(req,log)](#M-Authentication-RefreshToken-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.RefreshToken.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [SearchUserResponse](#T-Authentication-Shared-Responses-SearchUserResponse 'Authentication.Shared.Responses.SearchUserResponse')
  - [Values](#P-Authentication-Shared-Responses-SearchUserResponse-Values 'Authentication.Shared.Responses.SearchUserResponse.Values')
- [SignInName](#T-Authentication-Shared-Models-ADUser-SignInName 'Authentication.Shared.Models.ADUser.SignInName')
- [SignInName](#T-Authentication-Shared-Requests-CreateADUserParameters-SignInName 'Authentication.Shared.Requests.CreateADUserParameters.SignInName')
  - [Type](#P-Authentication-Shared-Models-ADUser-SignInName-Type 'Authentication.Shared.Models.ADUser.SignInName.Type')
  - [Value](#P-Authentication-Shared-Models-ADUser-SignInName-Value 'Authentication.Shared.Models.ADUser.SignInName.Value')
  - [Type](#P-Authentication-Shared-Requests-CreateADUserParameters-SignInName-Type 'Authentication.Shared.Requests.CreateADUserParameters.SignInName.Type')
  - [Value](#P-Authentication-Shared-Requests-CreateADUserParameters-SignInName-Value 'Authentication.Shared.Requests.CreateADUserParameters.SignInName.Value')
- [Startup](#T-Authentication-Startup 'Authentication.Startup')
  - [Configure(builder)](#M-Authentication-Startup-Configure-Microsoft-Azure-Functions-Extensions-DependencyInjection-IFunctionsHostBuilder- 'Authentication.Startup.Configure(Microsoft.Azure.Functions.Extensions.DependencyInjection.IFunctionsHostBuilder)')
- [String](#T-Extensions-String 'Extensions.String')
  - [EqualsIgnoreCase(str,other)](#M-Extensions-String-EqualsIgnoreCase-System-String,System-String- 'Extensions.String.EqualsIgnoreCase(System.String,System.String)')
  - [IsValidEmailAddress(address)](#M-Extensions-String-IsValidEmailAddress-System-String- 'Extensions.String.IsValidEmailAddress(System.String)')
  - [MD5(str)](#M-Extensions-String-MD5-System-String- 'Extensions.String.MD5(System.String)')
  - [ToBase64(str)](#M-Extensions-String-ToBase64-System-String- 'Extensions.String.ToBase64(System.String)')
- [TokenHelper](#T-Authentication-Shared-Utils-TokenHelper 'Authentication.Shared.Utils.TokenHelper')
  - [ISSUER](#F-Authentication-Shared-Utils-TokenHelper-ISSUER 'Authentication.Shared.Utils.TokenHelper.ISSUER')
  - [ValidateB2CToken(idToken,policy)](#M-Authentication-Shared-Utils-TokenHelper-ValidateB2CToken-System-String,System-String- 'Authentication.Shared.Utils.TokenHelper.ValidateB2CToken(System.String,System.String)')
  - [ValidateClientToken(token,secret,iss,sub)](#M-Authentication-Shared-Utils-TokenHelper-ValidateClientToken-System-String,System-String,System-String,System-String- 'Authentication.Shared.Utils.TokenHelper.ValidateClientToken(System.String,System.String,System.String,System.String)')
- [UpdateRole](#T-Authentication-UpdateRole 'Authentication.UpdateRole')
  - [Run(req,log)](#M-Authentication-UpdateRole-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger- 'Authentication.UpdateRole.Run(Microsoft.AspNetCore.Http.HttpRequest,Microsoft.Extensions.Logging.ILogger)')
- [User](#T-Authentication-Shared-Models-User 'Authentication.Shared.Models.User')
  - [City](#P-Authentication-Shared-Models-User-City 'Authentication.Shared.Models.User.City')
  - [Country](#P-Authentication-Shared-Models-User-Country 'Authentication.Shared.Models.User.Country')
  - [CreatedAt](#P-Authentication-Shared-Models-User-CreatedAt 'Authentication.Shared.Models.User.CreatedAt')
  - [Email](#P-Authentication-Shared-Models-User-Email 'Authentication.Shared.Models.User.Email')
  - [FirstName](#P-Authentication-Shared-Models-User-FirstName 'Authentication.Shared.Models.User.FirstName')
  - [Gateway](#P-Authentication-Shared-Models-User-Gateway 'Authentication.Shared.Models.User.Gateway')
  - [Id](#P-Authentication-Shared-Models-User-Id 'Authentication.Shared.Models.User.Id')
  - [IsEU](#P-Authentication-Shared-Models-User-IsEU 'Authentication.Shared.Models.User.IsEU')
  - [LastName](#P-Authentication-Shared-Models-User-LastName 'Authentication.Shared.Models.User.LastName')
  - [LastSignInIP](#P-Authentication-Shared-Models-User-LastSignInIP 'Authentication.Shared.Models.User.LastSignInIP')
  - [OrganisationName](#P-Authentication-Shared-Models-User-OrganisationName 'Authentication.Shared.Models.User.OrganisationName')
  - [Partition](#P-Authentication-Shared-Models-User-Partition 'Authentication.Shared.Models.User.Partition')
  - [Region](#P-Authentication-Shared-Models-User-Region 'Authentication.Shared.Models.User.Region')
  - [SubscriptionExpiredAt](#P-Authentication-Shared-Models-User-SubscriptionExpiredAt 'Authentication.Shared.Models.User.SubscriptionExpiredAt')
  - [Type](#P-Authentication-Shared-Models-User-Type 'Authentication.Shared.Models.User.Type')
  - [UpdatedAt](#P-Authentication-Shared-Models-User-UpdatedAt 'Authentication.Shared.Models.User.UpdatedAt')
  - [CreateOrUpdate()](#M-Authentication-Shared-Models-User-CreateOrUpdate 'Authentication.Shared.Models.User.CreateOrUpdate')
  - [GetByEmail(email)](#M-Authentication-Shared-Models-User-GetByEmail-System-String- 'Authentication.Shared.Models.User.GetByEmail(System.String)')
  - [GetById(id)](#M-Authentication-Shared-Models-User-GetById-System-String- 'Authentication.Shared.Models.User.GetById(System.String)')
- [UserGroupsResponse](#T-Authentication-Shared-Responses-UserGroupsResponse 'Authentication.Shared.Responses.UserGroupsResponse')
  - [GroupIds](#P-Authentication-Shared-Responses-UserGroupsResponse-GroupIds 'Authentication.Shared.Responses.UserGroupsResponse.GroupIds')
  - [OdataMetadata](#P-Authentication-Shared-Responses-UserGroupsResponse-OdataMetadata 'Authentication.Shared.Responses.UserGroupsResponse.OdataMetadata')

<a name='T-Authentication-Shared-ADAccess'></a>
## ADAccess `type`

##### Namespace

Authentication.Shared

##### Summary

Azure directory access helper class.
This class uses to manage all the tokens in Azure directory

<a name='M-Authentication-Shared-ADAccess-#ctor'></a>
### #ctor() `constructor`

##### Summary

Prevents a default instance of the [ADAccess](#T-Authentication-Shared-ADAccess 'Authentication.Shared.ADAccess') class from being created

##### Parameters

This constructor has no parameters.

<a name='F-Authentication-Shared-ADAccess-masterToken'></a>
### masterToken `constants`

##### Summary

The master token to call b2c api

<a name='P-Authentication-Shared-ADAccess-Instance'></a>
### Instance `property`

##### Summary

Gets singleton instance

<a name='M-Authentication-Shared-ADAccess-GetAccessToken-System-String,System-String-'></a>
### GetAccessToken(email,password) `method`

##### Summary

Get b2c access token from login with email and password

##### Returns

ADToken class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |
| password | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user password |

<a name='M-Authentication-Shared-ADAccess-GetMasterKey'></a>
### GetMasterKey() `method`

##### Summary

Get master key

##### Returns

Master key

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-ADAccess-RefreshToken-System-String-'></a>
### RefreshToken(token) `method`

##### Summary

Refresh to get new access token

##### Returns

New access token

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | A refresh token |

<a name='M-Authentication-Shared-ADAccess-ValidateAccessToken-System-String-'></a>
### ValidateAccessToken(token) `method`

##### Summary

Validate B2C access token

##### Returns

Result, message, userid

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |

<a name='M-Authentication-Shared-ADAccess-ValidateClientToken-System-String-'></a>
### ValidateClientToken(token) `method`

##### Summary

Validate mobile client token

##### Returns

Result, message, payload

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | client token |

<a name='M-Authentication-Shared-ADAccess-ValidateIdToken-System-String-'></a>
### ValidateIdToken(token) `method`

##### Summary

Validate B2C Id token

##### Returns

Result, message, email

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Id token |

<a name='T-Authentication-Shared-Models-ADGroup'></a>
## ADGroup `type`

##### Namespace

Authentication.Shared.Models

##### Summary

AD Group
This class contains all the defined groups in b2c

<a name='P-Authentication-Shared-Models-ADGroup-Description'></a>
### Description `property`

##### Summary

Gets or sets description of group

<a name='P-Authentication-Shared-Models-ADGroup-Id'></a>
### Id `property`

##### Summary

Gets or sets id of group

<a name='P-Authentication-Shared-Models-ADGroup-Name'></a>
### Name `property`

##### Summary

Gets or sets name of group

<a name='P-Authentication-Shared-Models-ADGroup-Type'></a>
### Type `property`

##### Summary

Gets or sets object type

<a name='M-Authentication-Shared-Models-ADGroup-AddUser-System-String-'></a>
### AddUser(userId) `method`

##### Summary

Add user into group

##### Returns

True if success

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Models-ADGroup-FindById-System-String-'></a>
### FindById(id) `method`

##### Summary

Get ADGroup instance from its id

##### Returns

ADGroup instance

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | group id |

<a name='M-Authentication-Shared-Models-ADGroup-FindByName-System-String-'></a>
### FindByName(name) `method`

##### Summary

Get ADGroup instance from its name

##### Returns

ADGroup instance

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | group name |

<a name='M-Authentication-Shared-Models-ADGroup-GetAllGroups'></a>
### GetAllGroups() `method`

##### Summary

Get all b2c groups
If the group list is in the cache, then return it. Otherwise refresh cache and return

##### Returns

List of groups or null

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ADGroup-GetOrCreateAdminPermission-System-String-'></a>
### GetOrCreateAdminPermission(table) `method`

##### Summary

Get or create Cosmos permission (resource tokens) for admin role.
It will create Cosmos permision if does not exist

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| table | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The cosmos table |

<a name='M-Authentication-Shared-Models-ADGroup-GetOrCreatePermission-Authentication-Shared-Models-CosmosRolePermission-'></a>
### GetOrCreatePermission(rolePermission) `method`

##### Summary

Get or create Cosmos permission (resource tokens) base on role (AD group)
It will create Cosmos permision if does not exist

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| rolePermission | [Authentication.Shared.Models.CosmosRolePermission](#T-Authentication-Shared-Models-CosmosRolePermission 'Authentication.Shared.Models.CosmosRolePermission') | The role permission record |

<a name='M-Authentication-Shared-Models-ADGroup-GetPermissions'></a>
### GetPermissions() `method`

##### Summary

Get cosmos permissions of group
If the permission list is in the cache, then return it. Otherwise refresh cache and return

##### Returns

List of permissions

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ADGroup-HasUser-System-String-'></a>
### HasUser(userId) `method`

##### Summary

Check if group has a user

##### Returns

True if success

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Models-ADGroup-RemoveUser-System-String-'></a>
### RemoveUser(userId) `method`

##### Summary

Remove user from a group

##### Returns

true if success

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='T-Authentication-Shared-Models-ADToken'></a>
## ADToken `type`

##### Namespace

Authentication.Shared.Models

##### Summary

AD User Token
This token uses to access b2c api

<a name='P-Authentication-Shared-Models-ADToken-AccessToken'></a>
### AccessToken `property`

##### Summary

Gets or sets access token

<a name='P-Authentication-Shared-Models-ADToken-ExpiresIn'></a>
### ExpiresIn `property`

##### Summary

Gets or sets expires in

<a name='P-Authentication-Shared-Models-ADToken-ExpiresOn'></a>
### ExpiresOn `property`

##### Summary

Gets or sets expires on

<a name='P-Authentication-Shared-Models-ADToken-IsExpired'></a>
### IsExpired `property`

##### Summary

Gets a value indicating whether this token is expired

<a name='P-Authentication-Shared-Models-ADToken-NotBefore'></a>
### NotBefore `property`

##### Summary

Gets or sets not before

<a name='P-Authentication-Shared-Models-ADToken-RefreshToken'></a>
### RefreshToken `property`

##### Summary

Gets or sets refresh token

<a name='P-Authentication-Shared-Models-ADToken-Resource'></a>
### Resource `property`

##### Summary

Gets or sets resource url

<a name='T-Authentication-Shared-Models-ADUser'></a>
## ADUser `type`

##### Namespace

Authentication.Shared.Models

##### Summary

AD user
It contains information of AD user

<a name='P-Authentication-Shared-Models-ADUser-AccountEnabled'></a>
### AccountEnabled `property`

##### Summary

Gets or sets a value indicating whether account is enabled

<a name='P-Authentication-Shared-Models-ADUser-ObjectId'></a>
### ObjectId `property`

##### Summary

Gets or sets object id

<a name='P-Authentication-Shared-Models-ADUser-PasswordPolicies'></a>
### PasswordPolicies `property`

##### Summary

Gets or sets password policies

<a name='P-Authentication-Shared-Models-ADUser-SignInNames'></a>
### SignInNames `property`

##### Summary

Gets or sets sign in name

<a name='P-Authentication-Shared-Models-ADUser-UserType'></a>
### UserType `property`

##### Summary

Gets or sets user type

<a name='M-Authentication-Shared-Models-ADUser-FindByEmail-System-String-'></a>
### FindByEmail(email) `method`

##### Summary

Find user by email

##### Returns

ADUser class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | email param |

<a name='M-Authentication-Shared-Models-ADUser-FindById-System-String-'></a>
### FindById(id) `method`

##### Summary

Find user by id

##### Returns

ADUser class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Models-ADUser-FindOrCreate-System-String,System-String-'></a>
### FindOrCreate(email,name) `method`

##### Summary

Find or create a user
If user already exists, then return result = true and user info
Otherwise create a new user, then return result = false and user info

##### Returns

(Result, ADUser). Result is true if user already exist, otherwise is false

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user name |

<a name='M-Authentication-Shared-Models-ADUser-GetOrCreateAdminPermissions-Authentication-Shared-Models-CosmosRolePermission-'></a>
### GetOrCreateAdminPermissions(rolePermission) `method`

##### Summary

Get or create Cosmos permission for an admin user

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| rolePermission | [Authentication.Shared.Models.CosmosRolePermission](#T-Authentication-Shared-Models-CosmosRolePermission 'Authentication.Shared.Models.CosmosRolePermission') | The role permission record |

<a name='M-Authentication-Shared-Models-ADUser-GetOrCreateSharePermissions-Authentication-Shared-Models-Connection-'></a>
### GetOrCreateSharePermissions(connection) `method`

##### Summary

Get or create shared cosmos permission base on Connection record

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| connection | [Authentication.Shared.Models.Connection](#T-Authentication-Shared-Models-Connection 'Authentication.Shared.Models.Connection') | The role permission record |

<a name='M-Authentication-Shared-Models-ADUser-GetOrCreateShareProfilePermissions-Authentication-Shared-Models-Connection-'></a>
### GetOrCreateShareProfilePermissions(connection) `method`

##### Summary

Get or create cosmos permission for shared Profile table

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| connection | [Authentication.Shared.Models.Connection](#T-Authentication-Shared-Models-Connection 'Authentication.Shared.Models.Connection') | The connection record |

<a name='M-Authentication-Shared-Models-ADUser-GetOrCreateUserPermissions-Authentication-Shared-Models-CosmosRolePermission-'></a>
### GetOrCreateUserPermissions(rolePermission) `method`

##### Summary

Get or create Cosmos permission for a user

##### Returns

A permission class or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| rolePermission | [Authentication.Shared.Models.CosmosRolePermission](#T-Authentication-Shared-Models-CosmosRolePermission 'Authentication.Shared.Models.CosmosRolePermission') | The role permission record |

<a name='M-Authentication-Shared-Models-ADUser-GetPermissions-System-String-'></a>
### GetPermissions() `method`

##### Summary

Get cosmos permission of this user

##### Returns

List of permissions

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ADUser-GroupIds'></a>
### GroupIds() `method`

##### Summary

Get group id list of current user

##### Returns

List of group id

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ADUser-UpdateGroup-System-String-'></a>
### UpdateGroup(newGroupName) `method`

##### Summary

Change group for current user

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| newGroupName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') |  |

<a name='T-Authentication-Shared-Responses-APIResult'></a>
## APIResult `type`

##### Namespace

Authentication.Shared.Responses

##### Summary

API result

<a name='P-Authentication-Shared-Responses-APIResult-Metadata'></a>
### Metadata `property`

##### Summary

Gets or sets medadata

<a name='P-Authentication-Shared-Responses-APIResult-Value'></a>
### Value `property`

##### Summary

Gets or sets a value indicating whether result is success

<a name='T-Authentication-Shared-Requests-AddUserToGroupParameter'></a>
## AddUserToGroupParameter `type`

##### Namespace

Authentication.Shared.Requests

##### Summary

Add an user into group param
This is serializable class to send on restful

<a name='P-Authentication-Shared-Requests-AddUserToGroupParameter-Url'></a>
### Url `property`

##### Summary

Gets or sets url

<a name='T-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi'></a>
## AutoGeneratedIAzureGraphRestApi `type`

##### Namespace

Authentication.Shared.Services

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder-'></a>
### #ctor() `constructor`

##### Summary

*Inherit from parent.*

##### Parameters

This constructor has no parameters.

<a name='P-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Client'></a>
### Client `property`

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#AddUserToGroup-System-String,System-String,System-String,Authentication-Shared-Requests-AddUserToGroupParameter-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#AddUserToGroup() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#CreateUser-System-String,System-String,Authentication-Shared-Requests-CreateADUserParameters-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#CreateUser() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetAllGroups-System-String,System-String-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#GetAllGroups() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetUserById-System-String,System-String,System-String-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#GetUserById() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#GetUserGroups-System-String,System-String,System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#GetUserGroups() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#IsMemberOf-System-String,System-String,Authentication-Shared-Requests-IsMemberOfParam-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#IsMemberOf() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#RemoveUserFromGroup-System-String,System-String,System-String,System-String-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#RemoveUserFromGroup() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIAzureGraphRestApi-Authentication#Shared#Services#IAzureGraphRestApi#SearchUser-System-String,System-String,System-String-'></a>
### Authentication#Shared#Services#IAzureGraphRestApi#SearchUser() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Authentication-Shared-Services-AutoGeneratedIB2CRestApi'></a>
## AutoGeneratedIB2CRestApi `type`

##### Namespace

Authentication.Shared.Services

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder-'></a>
### #ctor() `constructor`

##### Summary

*Inherit from parent.*

##### Parameters

This constructor has no parameters.

<a name='P-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Client'></a>
### Client `property`

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Authentication#Shared#Services#IB2CRestApi#GetAccessToken-System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#IB2CRestApi#GetAccessToken() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedIB2CRestApi-Authentication#Shared#Services#IB2CRestApi#RefreshToken-System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#IB2CRestApi#RefreshToken() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi'></a>
## AutoGeneratedMicrosoftServiceIMicrosoftRestApi `type`

##### Namespace

Authentication.Shared.Services

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-#ctor-System-Net-Http-HttpClient,Refit-IRequestBuilder-'></a>
### #ctor() `constructor`

##### Summary

*Inherit from parent.*

##### Parameters

This constructor has no parameters.

<a name='P-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Client'></a>
### Client `property`

##### Summary

*Inherit from parent.*

<a name='M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetAccessToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetAccessToken() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetMasterToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#GetMasterToken() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AutoGeneratedMicrosoftServiceIMicrosoftRestApi-Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#RefreshToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### Authentication#Shared#Services#MicrosoftService#IMicrosoftRestApi#RefreshToken() `method`

##### Summary

*Inherit from parent.*

##### Parameters

This method has no parameters.

<a name='T-Authentication-Shared-Configurations-AzureB2C'></a>
## AzureB2C `type`

##### Namespace

Authentication.Shared.Configurations

##### Summary

Azure B2C constants

<a name='F-Authentication-Shared-Configurations-AzureB2C-AdminClientId'></a>
### AdminClientId `constants`

##### Summary

admin client key

<a name='F-Authentication-Shared-Configurations-AzureB2C-AdminClientSecret'></a>
### AdminClientSecret `constants`

##### Summary

Admin secret key

<a name='F-Authentication-Shared-Configurations-AzureB2C-AdminGroup'></a>
### AdminGroup `constants`

##### Summary

The admin group name

<a name='F-Authentication-Shared-Configurations-AzureB2C-AuthPolicy'></a>
### AuthPolicy `constants`

##### Summary

ROPC auth policy

<a name='F-Authentication-Shared-Configurations-AzureB2C-B2CClientId'></a>
### B2CClientId `constants`

##### Summary

B2C client id

<a name='F-Authentication-Shared-Configurations-AzureB2C-B2CUrl'></a>
### B2CUrl `constants`

##### Summary

B2C resource url

<a name='F-Authentication-Shared-Configurations-AzureB2C-BearerAuthentication'></a>
### BearerAuthentication `constants`

##### Summary

Bearer authentication header

<a name='F-Authentication-Shared-Configurations-AzureB2C-GrantTypeCredentials'></a>
### GrantTypeCredentials `constants`

##### Summary

Credentials grant type

<a name='F-Authentication-Shared-Configurations-AzureB2C-GrantTypePassword'></a>
### GrantTypePassword `constants`

##### Summary

Password grant type

<a name='F-Authentication-Shared-Configurations-AzureB2C-GrantTypeRefreshToken'></a>
### GrantTypeRefreshToken `constants`

##### Summary

Refresh token grant type

<a name='F-Authentication-Shared-Configurations-AzureB2C-GraphResource'></a>
### GraphResource `constants`

##### Summary

Azure Graph resource url

<a name='F-Authentication-Shared-Configurations-AzureB2C-GuestGroup'></a>
### GuestGroup `constants`

##### Summary

The guest group name

<a name='F-Authentication-Shared-Configurations-AzureB2C-IdTokenType'></a>
### IdTokenType `constants`

##### Summary

Id token type

<a name='F-Authentication-Shared-Configurations-AzureB2C-MicorsoftAuthUrl'></a>
### MicorsoftAuthUrl `constants`

##### Summary

Authentication url

<a name='F-Authentication-Shared-Configurations-AzureB2C-NewGroup'></a>
### NewGroup `constants`

##### Summary

The new group name

<a name='F-Authentication-Shared-Configurations-AzureB2C-PasswordPrefix'></a>
### PasswordPrefix `constants`

##### Summary

Password prefix to by pass the password complexity requirements

<a name='F-Authentication-Shared-Configurations-AzureB2C-PasswordSecretKey'></a>
### PasswordSecretKey `constants`

##### Summary

Secret key to generate password

<a name='F-Authentication-Shared-Configurations-AzureB2C-SignInSignUpPolicy'></a>
### SignInSignUpPolicy `constants`

##### Summary

Sign in sign up policy

<a name='F-Authentication-Shared-Configurations-AzureB2C-TenantId'></a>
### TenantId `constants`

##### Summary

Tenant id

<a name='F-Authentication-Shared-Configurations-AzureB2C-TenantName'></a>
### TenantName `constants`

##### Summary

Tenant name

<a name='F-Authentication-Shared-Configurations-AzureB2C-TokenType'></a>
### TokenType `constants`

##### Summary

Token type

<a name='T-Authentication-Shared-Services-AzureB2CService'></a>
## AzureB2CService `type`

##### Namespace

Authentication.Shared.Services

##### Summary

B2C Azure service
A singleton helper to call b2c and graph APIs

<a name='M-Authentication-Shared-Services-AzureB2CService-#ctor'></a>
### #ctor() `constructor`

##### Summary

Prevents a default instance of the [AzureB2CService](#T-Authentication-Shared-Services-AzureB2CService 'Authentication.Shared.Services.AzureB2CService') class from being created

##### Parameters

This constructor has no parameters.

<a name='F-Authentication-Shared-Services-AzureB2CService-azureGraphRestApi'></a>
### azureGraphRestApi `constants`

##### Summary

Azure graph service

<a name='F-Authentication-Shared-Services-AzureB2CService-b2cHttpClient'></a>
### b2cHttpClient `constants`

##### Summary

B2C Http client

<a name='F-Authentication-Shared-Services-AzureB2CService-b2cRestApi'></a>
### b2cRestApi `constants`

##### Summary

B2C service

<a name='F-Authentication-Shared-Services-AzureB2CService-graphHttpClient'></a>
### graphHttpClient `constants`

##### Summary

Azure graph Http client

<a name='P-Authentication-Shared-Services-AzureB2CService-Instance'></a>
### Instance `property`

##### Summary

Gets singleton instance

<a name='M-Authentication-Shared-Services-AzureB2CService-AddUserToGroup-System-String,System-String-'></a>
### AddUserToGroup(groupId,userId) `method`

##### Summary

Add an user into group

##### Returns

true if success

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| groupId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | group id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Services-AzureB2CService-CreateADUser-Authentication-Shared-Requests-CreateADUserParameters-'></a>
### CreateADUser(parameters) `method`

##### Summary

Create an AD User

##### Returns

AD User

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| parameters | [Authentication.Shared.Requests.CreateADUserParameters](#T-Authentication-Shared-Requests-CreateADUserParameters 'Authentication.Shared.Requests.CreateADUserParameters') | user creation parameters |

<a name='M-Authentication-Shared-Services-AzureB2CService-GetADUserByEmail-System-String-'></a>
### GetADUserByEmail(email) `method`

##### Summary

Get an AD User

##### Returns

AD User

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |

<a name='M-Authentication-Shared-Services-AzureB2CService-GetAllGroups'></a>
### GetAllGroups() `method`

##### Summary

Get all groups of tenant

##### Returns

Groups response

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-AzureB2CService-GetB2CAccessToken-System-String,System-String-'></a>
### GetB2CAccessToken(email,password) `method`

##### Summary

Get access token in B2C policy

##### Returns

ADToken class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |
| password | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user password |

<a name='M-Authentication-Shared-Services-AzureB2CService-GetUserById-System-String-'></a>
### GetUserById())) `method`

##### Summary

Get user

##### Returns

[ADUser](#T-Authentication-Shared-Models-ADUser 'Authentication.Shared.Models.ADUser') class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| )) | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access ) |

<a name='M-Authentication-Shared-Services-AzureB2CService-GetUserGroups-System-String-'></a>
### GetUserGroups(userId) `method`

##### Summary

Get all groups of user

##### Returns

list of group id

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Services-AzureB2CService-IsMemberOfGroup-System-String,System-String-'></a>
### IsMemberOfGroup(groupId,userId) `method`

##### Summary

Check if user is in group

##### Returns

True if user in group

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| groupId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | group id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Services-AzureB2CService-RefreshB2CToken-System-String-'></a>
### RefreshB2CToken(refreshToken) `method`

##### Summary

Refresh access token in B2C policy

##### Returns

ADToken class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| refreshToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | refresh token |

<a name='M-Authentication-Shared-Services-AzureB2CService-RemoveUserFromGroup-System-String,System-String-'></a>
### RemoveUserFromGroup(groupId,userId) `method`

##### Summary

Remove user from a group

##### Returns

true if success

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| groupId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | group id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='T-Authentication-CheckAccount'></a>
## CheckAccount `type`

##### Namespace

Authentication

##### Summary

Account existing checking azure function.
This function uses to check whether user email exist in b2c.
If it does, return that user info, otherwise create a new account with that email then add user into "New" group in b2c and return it

<a name='M-Authentication-CheckAccount-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to check whether user account is exist.
Parameters:
If a user with that email exist, then return that user, otherwise create a new user and add it into "New" group in b2c and return it

##### Returns

User result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-Shared-Configurations'></a>
## Configurations `type`

##### Namespace

Authentication.Shared

##### Summary

Configurations class
This class contains all the configurations constants

<a name='P-Authentication-Shared-Configurations-Configuration'></a>
### Configuration `property`

##### Summary

Gets or sets App configuration

<a name='T-Authentication-Shared-Models-Connection'></a>
## Connection `type`

##### Namespace

Authentication.Shared.Models

##### Summary

Cosmos connection modal
Contains all the properties

<a name='P-Authentication-Shared-Models-Connection-CreatedAt'></a>
### CreatedAt `property`

##### Summary

Gets or sets created at

<a name='P-Authentication-Shared-Models-Connection-Id'></a>
### Id `property`

##### Summary

Gets or sets id

<a name='P-Authentication-Shared-Models-Connection-IsReadOnly'></a>
### IsReadOnly `property`

##### Summary

Gets whether permission is read only

<a name='P-Authentication-Shared-Models-Connection-Partition'></a>
### Partition `property`

##### Summary

Gets or sets partition

<a name='P-Authentication-Shared-Models-Connection-Permission'></a>
### Permission `property`

##### Summary

Gets or sets permission

<a name='P-Authentication-Shared-Models-Connection-Profiles'></a>
### Profiles `property`

##### Summary

Gets or sets profiles

<a name='P-Authentication-Shared-Models-Connection-Status'></a>
### Status `property`

##### Summary

Gets or sets status
Available statues: accepted, cancelled

<a name='P-Authentication-Shared-Models-Connection-Table'></a>
### Table `property`

##### Summary

Gets or sets table

<a name='P-Authentication-Shared-Models-Connection-UpdatedAt'></a>
### UpdatedAt `property`

##### Summary

Gets or sets updated at

<a name='P-Authentication-Shared-Models-Connection-User1'></a>
### User1 `property`

##### Summary

Gets or sets user 1

<a name='P-Authentication-Shared-Models-Connection-User2'></a>
### User2 `property`

##### Summary

Gets or sets user 2

<a name='M-Authentication-Shared-Models-Connection-CreateOrUpdate'></a>
### CreateOrUpdate() `method`

##### Summary

Create or update Connection

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-CreatePermission'></a>
### CreatePermission() `method`

##### Summary

Create cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-CreateProfilePermission'></a>
### CreateProfilePermission() `method`

##### Summary

Create profile cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-GetPermission'></a>
### GetPermission() `method`

##### Summary

Get cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-GetProfilePermission'></a>
### GetProfilePermission() `method`

##### Summary

Get profile cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-QueryByShareUser-System-String-'></a>
### QueryByShareUser(userId) `method`

##### Summary

Query connection by shared user id

##### Returns

List of Connection

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Models-Connection-UpdatePermission'></a>
### UpdatePermission() `method`

##### Summary

Update cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-Connection-UpdateProfilePermission'></a>
### UpdateProfilePermission() `method`

##### Summary

Update profile cosmos permission

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='T-Authentication-Shared-Models-ConnectionToken'></a>
## ConnectionToken `type`

##### Namespace

Authentication.Shared.Models

##### Summary

Cosmos ConnectionToken modal
Contains all the properties

<a name='P-Authentication-Shared-Models-ConnectionToken-ChildFirstName'></a>
### ChildFirstName `property`

##### Summary

Gets or sets child first name

<a name='P-Authentication-Shared-Models-ConnectionToken-ChildLastName'></a>
### ChildLastName `property`

##### Summary

Gets or sets child last name

<a name='P-Authentication-Shared-Models-ConnectionToken-CreatedAt'></a>
### CreatedAt `property`

##### Summary

Gets or sets created at

<a name='P-Authentication-Shared-Models-ConnectionToken-Email'></a>
### Email `property`

##### Summary

Gets or sets email

<a name='P-Authentication-Shared-Models-ConnectionToken-FirstName'></a>
### FirstName `property`

##### Summary

Gets or sets first name

<a name='P-Authentication-Shared-Models-ConnectionToken-FromEmail'></a>
### FromEmail `property`

##### Summary

Gets or sets from email

<a name='P-Authentication-Shared-Models-ConnectionToken-FromFirstName'></a>
### FromFirstName `property`

##### Summary

Gets or sets from first name

<a name='P-Authentication-Shared-Models-ConnectionToken-FromId'></a>
### FromId `property`

##### Summary

Gets or sets from id

<a name='P-Authentication-Shared-Models-ConnectionToken-FromLastName'></a>
### FromLastName `property`

##### Summary

Gets or sets from last name

<a name='P-Authentication-Shared-Models-ConnectionToken-Id'></a>
### Id `property`

##### Summary

Gets or sets id

<a name='P-Authentication-Shared-Models-ConnectionToken-IsFromParent'></a>
### IsFromParent `property`

##### Summary

Gets or sets isFromParent

<a name='P-Authentication-Shared-Models-ConnectionToken-LastName'></a>
### LastName `property`

##### Summary

Gets or sets last name

<a name='P-Authentication-Shared-Models-ConnectionToken-Partition'></a>
### Partition `property`

##### Summary

Gets or sets partition

<a name='P-Authentication-Shared-Models-ConnectionToken-Permission'></a>
### Permission `property`

##### Summary

Gets or sets permission

<a name='P-Authentication-Shared-Models-ConnectionToken-State'></a>
### State `property`

##### Summary

Gets or sets state
Available states: "invited", "shared", "unshared"

<a name='P-Authentication-Shared-Models-ConnectionToken-ToId'></a>
### ToId `property`

##### Summary

Gets or sets to user id

<a name='P-Authentication-Shared-Models-ConnectionToken-Token'></a>
### Token `property`

##### Summary

Gets or sets token

<a name='P-Authentication-Shared-Models-ConnectionToken-Type'></a>
### Type `property`

##### Summary

Gets or sets type

<a name='P-Authentication-Shared-Models-ConnectionToken-UpdatedAt'></a>
### UpdatedAt `property`

##### Summary

Gets or sets updated at

<a name='P-Authentication-Shared-Models-ConnectionToken-Viewed'></a>
### Viewed `property`

##### Summary

Gets or sets viewed

<a name='M-Authentication-Shared-Models-ConnectionToken-CreateOrUpdate'></a>
### CreateOrUpdate() `method`

##### Summary

Create or update Connection

##### Returns

Permission class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ConnectionToken-GetById-System-String-'></a>
### GetById(id) `method`

##### Summary

Get record by id

##### Returns

ConnectionToken or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | record id |

<a name='M-Authentication-Shared-Models-ConnectionToken-ParentAccepted-Authentication-Shared-Models-User-'></a>
### ParentAccepted(professionalUser) `method`

##### Summary

Process when parent accepts the invitation
It creates new Connection record for sharing a table

##### Returns

Nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| professionalUser | [Authentication.Shared.Models.User](#T-Authentication-Shared-Models-User 'Authentication.Shared.Models.User') |  |

<a name='M-Authentication-Shared-Models-ConnectionToken-ParentDeny-Authentication-Shared-Models-User-'></a>
### ParentDeny(professionalUser) `method`

##### Summary

Process when parent denies the invitation
It will change the Connection record into cancelled or deny state

##### Returns

Nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| professionalUser | [Authentication.Shared.Models.User](#T-Authentication-Shared-Models-User 'Authentication.Shared.Models.User') |  |

<a name='M-Authentication-Shared-Models-ConnectionToken-ParentProcess'></a>
### ParentProcess() `method`

##### Summary

This function can handle the changes from ConnectionToken of parent
It only accepts invited, shared, unshared states

##### Returns

Nothing

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ConnectionToken-ProfessionalInvite-Authentication-Shared-Models-User-'></a>
### ProfessionalInvite(parentUser) `method`

##### Summary

Process when professional invites a parent

##### Returns

Nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| parentUser | [Authentication.Shared.Models.User](#T-Authentication-Shared-Models-User 'Authentication.Shared.Models.User') | Parent user |

<a name='M-Authentication-Shared-Models-ConnectionToken-ProfessionalProcess'></a>
### ProfessionalProcess() `method`

##### Summary

This function can handle the changes from ConnectionToken of professional
It only accepts invited, unshared states

##### Returns

Nothing

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-ConnectionToken-ProfessionalUnshare-Authentication-Shared-Models-User-'></a>
### ProfessionalUnshare(parentUser) `method`

##### Summary

Process when professional unshared the sharing connection with parent

##### Returns

Nothing

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| parentUser | [Authentication.Shared.Models.User](#T-Authentication-Shared-Models-User 'Authentication.Shared.Models.User') | Parent user |

<a name='T-Authentication-Shared-Configurations-Cosmos'></a>
## Cosmos `type`

##### Namespace

Authentication.Shared.Configurations

##### Summary

Cosmos db constants

<a name='F-Authentication-Shared-Configurations-Cosmos-AcceptedPermissions'></a>
### AcceptedPermissions `constants`

##### Summary

The permissions that can create

<a name='F-Authentication-Shared-Configurations-Cosmos-DatabaseId'></a>
### DatabaseId `constants`

##### Summary

Cosmos database id

<a name='F-Authentication-Shared-Configurations-Cosmos-DatabaseMasterKey'></a>
### DatabaseMasterKey `constants`

##### Summary

Cosmos master key

<a name='F-Authentication-Shared-Configurations-Cosmos-DatabaseUrl'></a>
### DatabaseUrl `constants`

##### Summary

Cosmos database url

<a name='F-Authentication-Shared-Configurations-Cosmos-DefaultPartition'></a>
### DefaultPartition `constants`

##### Summary

Default partition

<a name='F-Authentication-Shared-Configurations-Cosmos-PartitionKey'></a>
### PartitionKey `constants`

##### Summary

The partition key

<a name='F-Authentication-Shared-Configurations-Cosmos-ResourceTokenExpiration'></a>
### ResourceTokenExpiration `constants`

##### Summary

The resource token expiration

<a name='T-Authentication-Shared-Models-CosmosRolePermission'></a>
## CosmosRolePermission `type`

##### Namespace

Authentication.Shared.Models

##### Summary

Role and permission
This class is mapping from cosmos RolePermission table

<a name='P-Authentication-Shared-Models-CosmosRolePermission-Id'></a>
### Id `property`

##### Summary

Gets or sets id

<a name='P-Authentication-Shared-Models-CosmosRolePermission-Permission'></a>
### Permission `property`

##### Summary

Gets or sets permission

<a name='P-Authentication-Shared-Models-CosmosRolePermission-Role'></a>
### Role `property`

##### Summary

Gets or sets role

<a name='P-Authentication-Shared-Models-CosmosRolePermission-Table'></a>
### Table `property`

##### Summary

Gets or sets table

<a name='M-Authentication-Shared-Models-CosmosRolePermission-CreateCosmosPermission-System-String,System-String,System-String-'></a>
### CreateCosmosPermission(userId,permissionId,partition) `method`

##### Summary

Create cosmos permission

##### Returns

Permission class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |
| permissionId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | permission id |
| partition | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | partition key |

<a name='M-Authentication-Shared-Models-CosmosRolePermission-CreateCosmosUser-System-String-'></a>
### CreateCosmosUser(userId) `method`

##### Summary

Create cosmos user

##### Returns

User class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='M-Authentication-Shared-Models-CosmosRolePermission-GetAllTables'></a>
### GetAllTables() `method`

##### Summary

Get all the defined tables in database

##### Returns

List of table names

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-CosmosRolePermission-QueryByRole-System-String-'></a>
### QueryByRole(role) `method`

##### Summary

Query cosmos permissions by role

##### Returns

List of CosmosRolePermission

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| role | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | role name |

<a name='M-Authentication-Shared-Models-CosmosRolePermission-QueryByTable-System-String-'></a>
### QueryByTable(table) `method`

##### Summary

Query cosmos permissions by table name

##### Returns

List of CosmosRolePermission

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| table | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | table name |

<a name='T-Authentication-Shared-Requests-CreateADUserParameters'></a>
## CreateADUserParameters `type`

##### Namespace

Authentication.Shared.Requests

##### Summary

Create AD User parameters
This is serializable class to send on restful

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-AccountEnable'></a>
### AccountEnable `property`

##### Summary

Gets or sets a value indicating whether account enabled

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-CreationType'></a>
### CreationType `property`

##### Summary

Gets or sets createion type

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-DisplayName'></a>
### DisplayName `property`

##### Summary

Gets or sets display name

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-PasswordPolicies'></a>
### PasswordPolicies `property`

##### Summary

Gets or sets password policies

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-Profile'></a>
### Profile `property`

##### Summary

Gets or sets password profile

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-SignInNames'></a>
### SignInNames `property`

##### Summary

Gets or sets sign in name

<a name='T-Authentication-CreateRolePermission'></a>
## CreateRolePermission `type`

##### Namespace

Authentication

##### Summary

Create role permission azure function
This function uses to create cosmos user and permission
It return http success if there is no error, otherwise return http error

<a name='M-Authentication-CreateRolePermission-CreateRolePermissionForRoleAsync-System-String-'></a>
### CreateRolePermissionForRoleAsync(role) `method`

##### Summary

Create User (Role) and Permission in cosmos from input role

##### Returns

Task async

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| role | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user role |

<a name='M-Authentication-CreateRolePermission-CreateRolePermissionForTableAsync-System-String-'></a>
### CreateRolePermissionForTableAsync(table) `method`

##### Summary

Create User (Role) and Permission in cosmos from input table

##### Returns

Task async

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| table | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | table name |

<a name='M-Authentication-CreateRolePermission-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to create role and permission in cosmos.
Parameters:
"Role" and "table" parameters must not be existed at the same time.
If parameter "role" does exist, then look up the cosmos RolePermission table by this role and create cosmos user and permissions
If parameter "table" does exist, then look up the cosmos RolePermission table by this table and create cosmos user and permissions
Only "read" and "read-write" permissions in RolePermission table are processed.

##### Returns

Http success with code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-Shared-Services-DataService'></a>
## DataService `type`

##### Namespace

Authentication.Shared.Services

##### Summary

Cosmos database service
A singleton class that manages cosmos database

<a name='M-Authentication-Shared-Services-DataService-#ctor'></a>
### #ctor() `constructor`

##### Summary

Prevents a default instance of the [DataService](#T-Authentication-Shared-Services-DataService 'Authentication.Shared.Services.DataService') class from being created

##### Parameters

This constructor has no parameters.

<a name='F-Authentication-Shared-Services-DataService-client'></a>
### client `constants`

##### Summary

Cosmos document client

<a name='P-Authentication-Shared-Services-DataService-Instance'></a>
### Instance `property`

##### Summary

Gets singleton instance

<a name='M-Authentication-Shared-Services-DataService-ClearAllAsync'></a>
### ClearAllAsync() `method`

##### Summary

Clear all users and permissions

##### Returns

Async task

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-DataService-CreatePermission-System-String,System-String,System-Boolean,System-String,System-String-'></a>
### CreatePermission(userId,permissionId,readOnly,tableName,partition) `method`

##### Summary

Create cosmos permission if not exist

##### Returns

Permission class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |
| permissionId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | permission id |
| readOnly | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | is read only |
| tableName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | table name |
| partition | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | partition key |

<a name='M-Authentication-Shared-Services-DataService-CreateUser-System-String-'></a>
### CreateUser(userId) `method`

##### Summary

Create cosmos user if not exist

##### Returns

Cosmos user

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Cosmos user id |

<a name='M-Authentication-Shared-Services-DataService-GetAllTables'></a>
### GetAllTables() `method`

##### Summary

Get all the defined tables in database

##### Returns

List of table names

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-DataService-GetPermission-System-String,System-String-'></a>
### GetPermission(userId,permissionName) `method`

##### Summary

Get cosmos permission

##### Returns

Permission object

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |
| permissionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | permission name |

<a name='M-Authentication-Shared-Services-DataService-GetPermissions-System-String-'></a>
### GetPermissions(userId) `method`

##### Summary

Get cosmos permissions of user

##### Returns

List of permissions

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |

<a name='M-Authentication-Shared-Services-DataService-ListUsers'></a>
### ListUsers() `method`

##### Summary

List all users

##### Returns

List of users

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-DataService-QueryDocuments``1-System-String,Microsoft-Azure-Cosmos-QueryDefinition,System-String,System-Boolean-'></a>
### QueryDocuments\`\`1(collectionName,query,partition,crossPartition) `method`

##### Summary

Query documents from a collection

##### Returns

List of documents

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| collectionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | collection name |
| query | [Microsoft.Azure.Cosmos.QueryDefinition](#T-Microsoft-Azure-Cosmos-QueryDefinition 'Microsoft.Azure.Cosmos.QueryDefinition') | query paramter |
| partition | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | partition key |
| crossPartition | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | query cross partition |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| T | Document type |

<a name='M-Authentication-Shared-Services-DataService-RemovePermission-System-String,System-String-'></a>
### RemovePermission(userId,permissionName) `method`

##### Summary

Remove permission

##### Returns

Permission propert

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |
| permissionName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | permission name |

<a name='M-Authentication-Shared-Services-DataService-ReplacePermission-System-String,System-String,System-Boolean,System-String,System-String-'></a>
### ReplacePermission(userId,permissionId,readOnly,tableName,partition) `method`

##### Summary

Replace permission by a new one

##### Returns

Permission propert

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |
| permissionId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | permission id |
| readOnly | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | is read only |
| tableName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | table name |
| partition | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | partition key |

<a name='T-Extensions-Dictionary'></a>
## Dictionary `type`

##### Namespace

Extensions

##### Summary

Dictionary extensions

<a name='M-Extensions-Dictionary-AddIfNotEmpty``1-System-Collections-Generic-Dictionary{``0,System-Object},``0,System-Object-'></a>
### AddIfNotEmpty\`\`1(dictionary,key,value) `method`

##### Summary

Add value if it's not empty

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| dictionary | [System.Collections.Generic.Dictionary{\`\`0,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{``0,System.Object}') | input dictionary |
| key | [\`\`0](#T-``0 '``0') | key name |
| value | [System.Object](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Object 'System.Object') | key value |

##### Generic Types

| Name | Description |
| ---- | ----------- |
| TKey | key type |

<a name='T-Authentication-GetAccessToken'></a>
## GetAccessToken `type`

##### Namespace

Authentication

##### Summary

Get b2c access token azure function
This function uses to get a b2c access token from email/password or id token (from b2c login)

<a name='M-Authentication-GetAccessToken-GetAccessTokenFromIdToken-System-String-'></a>
### GetAccessTokenFromIdToken(idToken) `method`

##### Summary

Get b2c access token by id token

##### Returns

Access token result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| idToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | id token |

<a name='M-Authentication-GetAccessToken-GetAccessTokenFromLogin-System-String,System-String-'></a>
### GetAccessTokenFromLogin(email,password) `method`

##### Summary

Get b2c access token from email and password

##### Returns

Access token result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |
| password | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user password |

<a name='M-Authentication-GetAccessToken-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to get user access token by email and password or id token (from b2c login).
Parameters:
If id_token exists, then validate it with b2c custom policy (then get its email) and get the b2c access token by using its email and generated password 
If email and password exist, then get b2c access token by them

##### Returns

Access token result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-GetResourceTokens'></a>
## GetResourceTokens `type`

##### Namespace

Authentication

##### Summary

Get resource tokens azure function
This function uses to get the cosmos resource token permissions

<a name='M-Authentication-GetResourceTokens-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to get cosmos resource token permissions
Parameters:
If the access_token is missing, then return the guest permissions from cosmos
Otherwise validate the access token, then get user and email from the access token and finally get the cosmos permission for that user

##### Returns

Cosmos resource tokens result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-GetUserInfo'></a>
## GetUserInfo `type`

##### Namespace

Authentication

##### Summary

Get user info azure function
This function uses to get user info (User table) from cosmos

<a name='M-Authentication-GetUserInfo-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to get cosmos user record 
Parameters:

##### Returns

A cosmos User record

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-Shared-Responses-GroupsResponse'></a>
## GroupsResponse `type`

##### Namespace

Authentication.Shared.Responses

##### Summary

B2c Group list response

<a name='P-Authentication-Shared-Responses-GroupsResponse-Groups'></a>
### Groups `property`

##### Summary

Gets or sets group list

<a name='P-Authentication-Shared-Responses-GroupsResponse-Metadata'></a>
### Metadata `property`

##### Summary

Gets or sets metadata of group

<a name='T-Authentication-Shared-Utils-HttpHelper'></a>
## HttpHelper `type`

##### Namespace

Authentication.Shared.Utils

##### Summary

Http helper util class

<a name='M-Authentication-Shared-Utils-HttpHelper-CreateErrorResponse-System-String,System-Int32-'></a>
### CreateErrorResponse(message,statusCode) `method`

##### Summary

Create error json response

##### Returns

Error response

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| message | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Error message |
| statusCode | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | Http status code |

<a name='M-Authentication-Shared-Utils-HttpHelper-CreateSuccessResponse'></a>
### CreateSuccessResponse() `method`

##### Summary

Create success json response

##### Returns

Success response

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Utils-HttpHelper-GeneratePassword-System-String-'></a>
### GeneratePassword(email) `method`

##### Summary

Generate password from email + secret.
Need to add a prefix to by pass the password complexity requirements

##### Returns

password hash

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | email adress |

<a name='M-Authentication-Shared-Utils-HttpHelper-GetBearerAuthorization-System-String-'></a>
### GetBearerAuthorization(token) `method`

##### Summary

Get bearer authorization

##### Returns

Bearer authorization

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |

<a name='M-Authentication-Shared-Utils-HttpHelper-GetIpFromRequestHeaders-Microsoft-AspNetCore-Http-HttpRequest-'></a>
### GetIpFromRequestHeaders(request) `method`

##### Summary

Get id address from client request

##### Returns

ip address

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| request | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | the request |

<a name='M-Authentication-Shared-Utils-HttpHelper-VerifyAdminToken-System-String-'></a>
### VerifyAdminToken(authToken) `method`

##### Summary

Verify admin permission by access token

##### Returns

IActionResult if it has error. Otherwise return null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| authToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Authencation token |

<a name='T-Authentication-Shared-Utils-HttpLoggingHandler'></a>
## HttpLoggingHandler `type`

##### Namespace

Authentication.Shared.Utils

##### Summary

Logging handler class
This is the custom logging for Refit library

<a name='M-Authentication-Shared-Utils-HttpLoggingHandler-#ctor-System-Net-Http-HttpMessageHandler-'></a>
### #ctor(innerHandler) `constructor`

##### Summary

Initializes a new instance of the [HttpLoggingHandler](#T-Authentication-Shared-Utils-HttpLoggingHandler 'Authentication.Shared.Utils.HttpLoggingHandler') class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| innerHandler | [System.Net.Http.HttpMessageHandler](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.HttpMessageHandler 'System.Net.Http.HttpMessageHandler') | Message handler |

<a name='F-Authentication-Shared-Utils-HttpLoggingHandler-types'></a>
### types `constants`

##### Summary

Type array

<a name='M-Authentication-Shared-Utils-HttpLoggingHandler-IsTextBasedContentType-System-Net-Http-Headers-HttpHeaders-'></a>
### IsTextBasedContentType(headers) `method`

##### Summary

Check if header is text content type

##### Returns

True if content is text

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| headers | [System.Net.Http.Headers.HttpHeaders](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.Headers.HttpHeaders 'System.Net.Http.Headers.HttpHeaders') | http headers |

<a name='M-Authentication-Shared-Utils-HttpLoggingHandler-SendAsync-System-Net-Http-HttpRequestMessage,System-Threading-CancellationToken-'></a>
### SendAsync(request,cancellationToken) `method`

##### Summary

Send message as async

##### Returns

Response message

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| request | [System.Net.Http.HttpRequestMessage](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Net.Http.HttpRequestMessage 'System.Net.Http.HttpRequestMessage') | Http request |
| cancellationToken | [System.Threading.CancellationToken](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Threading.CancellationToken 'System.Threading.CancellationToken') | cancel token |

<a name='T-Authentication-Shared-Services-IAzureGraphRestApi'></a>
## IAzureGraphRestApi `type`

##### Namespace

Authentication.Shared.Services

##### Summary

Azure graph rest api
This interface contains the defined rest APIs of azure graph

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-AddUserToGroup-System-String,System-String,System-String,Authentication-Shared-Requests-AddUserToGroupParameter-'></a>
### AddUserToGroup(tenantId,groupId,accessToken,parameters) `method`

##### Summary

Add user into a group

##### Returns

API Result

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| groupId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Group id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |
| parameters | [Authentication.Shared.Requests.AddUserToGroupParameter](#T-Authentication-Shared-Requests-AddUserToGroupParameter 'Authentication.Shared.Requests.AddUserToGroupParameter') | parameters value |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-CreateUser-System-String,System-String,Authentication-Shared-Requests-CreateADUserParameters-'></a>
### CreateUser(tenantId,accessToken,param) `method`

##### Summary

Create AD user

##### Returns

Created ADUser

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |
| param | [Authentication.Shared.Requests.CreateADUserParameters](#T-Authentication-Shared-Requests-CreateADUserParameters 'Authentication.Shared.Requests.CreateADUserParameters') | parameters value |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-GetAllGroups-System-String,System-String-'></a>
### GetAllGroups(tenantId,accessToken) `method`

##### Summary

Get all groups of tenant

##### Returns

Group response

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-GetUserById-System-String,System-String,System-String-'></a>
### GetUserById(tenantId,userId,accessToken) `method`

##### Summary

Get AD User by id

##### Returns

ADUser class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Access token |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-GetUserGroups-System-String,System-String,System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### GetUserGroups(tenantId,userId,accessToken,parameters) `method`

##### Summary

Get groups of user

##### Returns

List of groups

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |
| parameters | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | parameters value |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-IsMemberOf-System-String,System-String,Authentication-Shared-Requests-IsMemberOfParam-'></a>
### IsMemberOf(tenantId,accessToken,param) `method`

##### Summary

Check if user is in a group

##### Returns

APIResult class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |
| param | [Authentication.Shared.Requests.IsMemberOfParam](#T-Authentication-Shared-Requests-IsMemberOfParam 'Authentication.Shared.Requests.IsMemberOfParam') | parameters value |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-RemoveUserFromGroup-System-String,System-String,System-String,System-String-'></a>
### RemoveUserFromGroup(tenantId,userId,groupId,accessToken) `method`

##### Summary

Remove user from a group

##### Returns

Api result

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |
| groupId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Group id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |

<a name='M-Authentication-Shared-Services-IAzureGraphRestApi-SearchUser-System-String,System-String,System-String-'></a>
### SearchUser(tenantId,accessToken,query) `method`

##### Summary

Search users

##### Returns

Search result

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Tenant id |
| accessToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | access token |
| query | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | search query |

<a name='T-Authentication-Shared-Services-IB2CRestApi'></a>
## IB2CRestApi `type`

##### Namespace

Authentication.Shared.Services

##### Summary

B2C rest api
This interface contains the defined rest APIs of b2c

<a name='M-Authentication-Shared-Services-IB2CRestApi-GetAccessToken-System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### GetAccessToken(parameters) `method`

##### Summary

Get access token

##### Returns

ADToken class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| parameters | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | Parameters dictionary |

<a name='M-Authentication-Shared-Services-IB2CRestApi-RefreshToken-System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### RefreshToken(parameters) `method`

##### Summary

Refresh token

##### Returns

ADToken class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| parameters | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | Parameters dictionary |

<a name='T-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi'></a>
## IMicrosoftRestApi `type`

##### Namespace

Authentication.Shared.Services.MicrosoftService

##### Summary

Microsoft online service
This interface contains the defined rest APIs of Microsoft online service

<a name='M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-GetAccessToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### GetAccessToken(tenantId,data) `method`

##### Summary

Get access token by app client and secret

##### Returns

Ad user access token

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | tenant id |
| data | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | parameter data |

<a name='M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-GetMasterToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### GetMasterToken(tenantId,data) `method`

##### Summary

Get master token by app client and secret

##### Returns

Ad user access token

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | tenant id |
| data | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | parameter data |

<a name='M-Authentication-Shared-Services-MicrosoftService-IMicrosoftRestApi-RefreshToken-System-String,System-Collections-Generic-Dictionary{System-String,System-Object}-'></a>
### RefreshToken(tenantId,data) `method`

##### Summary

Refresh token

##### Returns

Ad user access token

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| tenantId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | tenant id |
| data | [System.Collections.Generic.Dictionary{System.String,System.Object}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.Dictionary 'System.Collections.Generic.Dictionary{System.String,System.Object}') | parameter data |

<a name='T-Authentication-Shared-Requests-IsMemberOfParam'></a>
## IsMemberOfParam `type`

##### Namespace

Authentication.Shared.Requests

##### Summary

Is member of parameter
This is serializable class to send on restful

<a name='P-Authentication-Shared-Requests-IsMemberOfParam-GroupId'></a>
### GroupId `property`

##### Summary

Gets or sets group id

<a name='P-Authentication-Shared-Requests-IsMemberOfParam-MemeberId'></a>
### MemeberId `property`

##### Summary

Gets or sets member id

<a name='T-Authentication-Shared-Configurations-JWTToken'></a>
## JWTToken `type`

##### Namespace

Authentication.Shared.Configurations

##### Summary

JWT Token

<a name='F-Authentication-Shared-Configurations-JWTToken-TokenClientSecret'></a>
### TokenClientSecret `constants`

##### Summary

Mobile client secret key

<a name='F-Authentication-Shared-Configurations-JWTToken-TokenIssuer'></a>
### TokenIssuer `constants`

##### Summary

Token issuer

<a name='F-Authentication-Shared-Configurations-JWTToken-TokenSubject'></a>
### TokenSubject `constants`

##### Summary

Sub domain

<a name='T-Authentication-Shared-Utils-Logger'></a>
## Logger `type`

##### Namespace

Authentication.Shared.Utils

##### Summary

Logger class

<a name='P-Authentication-Shared-Utils-Logger-Log'></a>
### Log `property`

##### Summary

Gets or sets logger

<a name='T-Authentication-Shared-Services-MicrosoftService'></a>
## MicrosoftService `type`

##### Namespace

Authentication.Shared.Services

##### Summary

Microsoft Service
A singleton class that accesses the Microsoft APIs

<a name='M-Authentication-Shared-Services-MicrosoftService-#ctor'></a>
### #ctor() `constructor`

##### Summary

Prevents a default instance of the [MicrosoftService](#T-Authentication-Shared-Services-MicrosoftService 'Authentication.Shared.Services.MicrosoftService') class from being created

##### Parameters

This constructor has no parameters.

<a name='F-Authentication-Shared-Services-MicrosoftService-httpClient'></a>
### httpClient `constants`

##### Summary

Http client

<a name='F-Authentication-Shared-Services-MicrosoftService-service'></a>
### service `constants`

##### Summary

Rest api service

<a name='P-Authentication-Shared-Services-MicrosoftService-Instance'></a>
### Instance `property`

##### Summary

Gets singleton instance

<a name='M-Authentication-Shared-Services-MicrosoftService-GetAdminAccessToken-System-String,System-String-'></a>
### GetAdminAccessToken(email,password) `method`

##### Summary

Get access token for admin

##### Returns

[ADToken](#T-Authentication-Shared-Models-ADToken 'Authentication.Shared.Models.ADToken') class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user email |
| password | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | password param |

<a name='M-Authentication-Shared-Services-MicrosoftService-GetMasterToken'></a>
### GetMasterToken() `method`

##### Summary

Get master token

##### Returns

[ADToken](#T-Authentication-Shared-Models-ADToken 'Authentication.Shared.Models.ADToken') class

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Services-MicrosoftService-RefreshAdminAccessToken-System-String-'></a>
### RefreshAdminAccessToken(refreshToken) `method`

##### Summary

Refresh a token for admin

##### Returns

New ADToken

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| refreshToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | refresh token |

<a name='T-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile'></a>
## PasswordProfile `type`

##### Namespace

Authentication.Shared.Requests.CreateADUserParameters

##### Summary

The password profile config

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile-ForceChangePasswordNextLogin'></a>
### ForceChangePasswordNextLogin `property`

##### Summary

Gets or sets a value indicating whether force change password next login

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-PasswordProfile-Password'></a>
### Password `property`

##### Summary

Gets or sets password

<a name='T-Authentication-Shared-Models-Profile'></a>
## Profile `type`

##### Namespace

Authentication.Shared.Models

##### Summary

Profile cosmos modal

<a name='P-Authentication-Shared-Models-Profile-DateOfBirth'></a>
### DateOfBirth `property`

##### Summary

Gets or sets date of birth

<a name='P-Authentication-Shared-Models-Profile-FirstName'></a>
### FirstName `property`

##### Summary

Gets or sets first name

<a name='P-Authentication-Shared-Models-Profile-Gender'></a>
### Gender `property`

##### Summary

Gets or sets gender

<a name='P-Authentication-Shared-Models-Profile-Id'></a>
### Id `property`

##### Summary

Gets or sets id

<a name='P-Authentication-Shared-Models-Profile-LastName'></a>
### LastName `property`

##### Summary

Gets or sets last name

<a name='P-Authentication-Shared-Models-Profile-Partition'></a>
### Partition `property`

##### Summary

Gets or sets partition

<a name='P-Authentication-Shared-Models-Profile-RecommendProgram'></a>
### RecommendProgram `property`

##### Summary

Gets or sets last name

<a name='P-Authentication-Shared-Models-Profile-UserId'></a>
### UserId `property`

##### Summary

Gets or sets last name

<a name='M-Authentication-Shared-Models-Profile-GetById-System-String,System-String-'></a>
### GetById(userId,profileId) `method`

##### Summary

Get a profile by id

##### Returns

Profile record or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |
| profileId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | Profile id |

<a name='M-Authentication-Shared-Models-Profile-GetByUserId-System-String-'></a>
### GetByUserId(userId) `method`

##### Summary

Get list of profile by user id

##### Returns

List of profiles

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| userId | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User id |

<a name='T-Authentication-RefreshToken'></a>
## RefreshToken `type`

##### Namespace

Authentication

##### Summary

Refresh token azure function
This function uses to refresh the b2c access token by refresh token

<a name='M-Authentication-RefreshToken-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to refresh token
Parameters:
This function will call b2c api to get an access token from refresh token in client

##### Returns

Access token result with http code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-Shared-Responses-SearchUserResponse'></a>
## SearchUserResponse `type`

##### Namespace

Authentication.Shared.Responses

##### Summary

Search user response

<a name='P-Authentication-Shared-Responses-SearchUserResponse-Values'></a>
### Values `property`

##### Summary

Gets or sets ad user list

<a name='T-Authentication-Shared-Models-ADUser-SignInName'></a>
## SignInName `type`

##### Namespace

Authentication.Shared.Models.ADUser

##### Summary

Sign in name config

<a name='T-Authentication-Shared-Requests-CreateADUserParameters-SignInName'></a>
## SignInName `type`

##### Namespace

Authentication.Shared.Requests.CreateADUserParameters

##### Summary

Sign in name config

<a name='P-Authentication-Shared-Models-ADUser-SignInName-Type'></a>
### Type `property`

##### Summary

Gets or sets type

<a name='P-Authentication-Shared-Models-ADUser-SignInName-Value'></a>
### Value `property`

##### Summary

Gets or sets value

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-SignInName-Type'></a>
### Type `property`

##### Summary

Gets or sets type

<a name='P-Authentication-Shared-Requests-CreateADUserParameters-SignInName-Value'></a>
### Value `property`

##### Summary

Gets or sets value

<a name='T-Authentication-Startup'></a>
## Startup `type`

##### Namespace

Authentication

##### Summary

App function starting up class
This is the first point before all the functions start

<a name='M-Authentication-Startup-Configure-Microsoft-Azure-Functions-Extensions-DependencyInjection-IFunctionsHostBuilder-'></a>
### Configure(builder) `method`

##### Summary

Configure the app dependencies injection

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| builder | [Microsoft.Azure.Functions.Extensions.DependencyInjection.IFunctionsHostBuilder](#T-Microsoft-Azure-Functions-Extensions-DependencyInjection-IFunctionsHostBuilder 'Microsoft.Azure.Functions.Extensions.DependencyInjection.IFunctionsHostBuilder') | Function builder to register dependencies injection |

<a name='T-Extensions-String'></a>
## String `type`

##### Namespace

Extensions

##### Summary

String extension methods

<a name='M-Extensions-String-EqualsIgnoreCase-System-String,System-String-'></a>
### EqualsIgnoreCase(str,other) `method`

##### Summary

Compare string ignore case

##### Returns

true if string is equal

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| str | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | input string |
| other | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | other string |

<a name='M-Extensions-String-IsValidEmailAddress-System-String-'></a>
### IsValidEmailAddress(address) `method`

##### Summary

Check is email is valid

##### Returns

true if email is valid

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| address | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | email address |

<a name='M-Extensions-String-MD5-System-String-'></a>
### MD5(str) `method`

##### Summary

Generate md5 hash

##### Returns

md5 hash

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| str | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | input string |

<a name='M-Extensions-String-ToBase64-System-String-'></a>
### ToBase64(str) `method`

##### Summary

Get base64 of utf string

##### Returns

Base64 string

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| str | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | input string |

<a name='T-Authentication-Shared-Utils-TokenHelper'></a>
## TokenHelper `type`

##### Namespace

Authentication.Shared.Utils

##### Summary

Token helper
This class uses to validate the tokens

<a name='F-Authentication-Shared-Utils-TokenHelper-ISSUER'></a>
### ISSUER `constants`

##### Summary

Azure B2C Issuer

<a name='M-Authentication-Shared-Utils-TokenHelper-ValidateB2CToken-System-String,System-String-'></a>
### ValidateB2CToken(idToken,policy) `method`

##### Summary

Validate b2c token by the custom b2c policy
Each policy has its own issuer, signing keys.. so we need to make sure all information is correct

##### Returns

claim principal

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| idToken | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | id token. This can be decode by JWT |
| policy | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | custom policy name |

<a name='M-Authentication-Shared-Utils-TokenHelper-ValidateClientToken-System-String,System-String,System-String,System-String-'></a>
### ValidateClientToken(token,secret,iss,sub) `method`

##### Summary

Validate client token from mobile by checking issuer and subject of token
We use JWT to validate the token

##### Returns

result, message, payload

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| token | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | token id |
| secret | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | secret key |
| iss | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | issuer param |
| sub | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | subject param |

<a name='T-Authentication-UpdateRole'></a>
## UpdateRole `type`

##### Namespace

Authentication

##### Summary

Update user role (group) azure function
This function uses to update user role. It also removes all the existing roles of user before assign to new role

<a name='M-Authentication-UpdateRole-Run-Microsoft-AspNetCore-Http-HttpRequest,Microsoft-Extensions-Logging-ILogger-'></a>
### Run(req,log) `method`

##### Summary

A http (Get, Post) method to update user role
Parameters:
This function will validate the email, then get ADUser by that email if it is valid.
All the existing roles of user are removed, then assign user to the new role

##### Returns

Http success with code 200 if no error, otherwise return http error

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| req | [Microsoft.AspNetCore.Http.HttpRequest](#T-Microsoft-AspNetCore-Http-HttpRequest 'Microsoft.AspNetCore.Http.HttpRequest') | HttpRequest type. It does contains parameters, headers... |
| log | [Microsoft.Extensions.Logging.ILogger](#T-Microsoft-Extensions-Logging-ILogger 'Microsoft.Extensions.Logging.ILogger') | The logger instance |

<a name='T-Authentication-Shared-Models-User'></a>
## User `type`

##### Namespace

Authentication.Shared.Models

##### Summary

Cosmos user modal

<a name='P-Authentication-Shared-Models-User-City'></a>
### City `property`

##### Summary

Gets or sets city

<a name='P-Authentication-Shared-Models-User-Country'></a>
### Country `property`

##### Summary

Gets or sets country

<a name='P-Authentication-Shared-Models-User-CreatedAt'></a>
### CreatedAt `property`

##### Summary

Gets or sets created at

<a name='P-Authentication-Shared-Models-User-Email'></a>
### Email `property`

##### Summary

Gets or sets email

<a name='P-Authentication-Shared-Models-User-FirstName'></a>
### FirstName `property`

##### Summary

Gets or sets first name

<a name='P-Authentication-Shared-Models-User-Gateway'></a>
### Gateway `property`

##### Summary

Gets or sets gateway

<a name='P-Authentication-Shared-Models-User-Id'></a>
### Id `property`

##### Summary

Gets or sets id

<a name='P-Authentication-Shared-Models-User-IsEU'></a>
### IsEU `property`

##### Summary

Gets or sets is EU

<a name='P-Authentication-Shared-Models-User-LastName'></a>
### LastName `property`

##### Summary

Gets or sets last name

<a name='P-Authentication-Shared-Models-User-LastSignInIP'></a>
### LastSignInIP `property`

##### Summary

Gets or sets last sign in ip

<a name='P-Authentication-Shared-Models-User-OrganisationName'></a>
### OrganisationName `property`

##### Summary

Gets or sets organisation name

<a name='P-Authentication-Shared-Models-User-Partition'></a>
### Partition `property`

##### Summary

Gets or sets partition

<a name='P-Authentication-Shared-Models-User-Region'></a>
### Region `property`

##### Summary

Gets or sets region

<a name='P-Authentication-Shared-Models-User-SubscriptionExpiredAt'></a>
### SubscriptionExpiredAt `property`

##### Summary

Gets or sets subscription expired at

<a name='P-Authentication-Shared-Models-User-Type'></a>
### Type `property`

##### Summary

Gets or sets type

<a name='P-Authentication-Shared-Models-User-UpdatedAt'></a>
### UpdatedAt `property`

##### Summary

Gets or sets updated at

<a name='M-Authentication-Shared-Models-User-CreateOrUpdate'></a>
### CreateOrUpdate() `method`

##### Summary

Create or update a user record

##### Returns

User record

##### Parameters

This method has no parameters.

<a name='M-Authentication-Shared-Models-User-GetByEmail-System-String-'></a>
### GetByEmail(email) `method`

##### Summary

Search user by email

##### Returns

User record or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| email | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | User email |

<a name='M-Authentication-Shared-Models-User-GetById-System-String-'></a>
### GetById(id) `method`

##### Summary

Get a user by id

##### Returns

User record or null

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| id | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | user id |

<a name='T-Authentication-Shared-Responses-UserGroupsResponse'></a>
## UserGroupsResponse `type`

##### Namespace

Authentication.Shared.Responses

##### Summary

User group response

<a name='P-Authentication-Shared-Responses-UserGroupsResponse-GroupIds'></a>
### GroupIds `property`

##### Summary

Gets or sets group id list

<a name='P-Authentication-Shared-Responses-UserGroupsResponse-OdataMetadata'></a>
### OdataMetadata `property`

##### Summary

Gets or sets medadata
