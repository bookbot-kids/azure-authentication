# azure-authentication
- A backend framework to setup authentication in [Azure B2C Active Directory] (https://docs.microsoft.com/en-us/azure/active-directory-b2c/overview) and sharing cosmos database 
- Features:
    + Support to authenticate B2c users, allow to custom authenticate flow and ui.
    + Sign in passwordless. User only needs email for authentication, an OTP will be sent to user's email
    + Manage B2C users and groups
    + Manage query, access and share [Cosmos] (https://docs.microsoft.com/en-us/azure/cosmos-db/) data table for B2C users

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

## Documentation
### Notices
- All azure functions must be secured by [API key](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp#api-key-authorization), so all functions must include `code` parameter
- Read about [Azure B2C token](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tokens-overview)
### Client flow
- Call [CheckAccount](#CheckAccount) to check if user exist (and create if not exist)
- Authenticate with azure b2c to get the id token. See [document](https://docs.microsoft.com/en-us/azure/active-directory-b2c/quickstart-web-app-dotnet) 
- Use id token to call [GetRefreshAndAccessToken](#GetRefreshAndAccessToken) and save the refresh token to access other APIs later
- Use refresh token to call [GetUserInfo](#GetUserInfo) to get User info in cosmos, or call [RefreshToken](#RefreshToken) to get a new refresh token
### User authentication functions
 - [CheckAccount](#CheckAccount)
 - [GetRefreshAndAccessToken](#GetRefreshAndAccessToken)
 - [GetResourceTokens](#GetResourceTokens)
 - [GetUserInfo](#GetUserInfo)
- [RefreshToken](#RefreshToken)
### Admin functions
- [CreateRolePermission](#CreateRolePermission)
- [UpdateRole](#UpdateRole)
___ 
#### CheckAccount 
- Http GET
- Check whether user account is exist. If a user with that email exist, then return that user, otherwise create a new user and add it into "New" group in b2c and return it
- Parameters:
	+ email: user email to check 
- Response:
	+ Success (200)
	```
	{
		"success": true,
		"exist": true // existing or new user
		"user": {} // ADUser object
	}
	```  
	+ Error 400: 
		+ email is missing or invalid	
- Example:
  + `curl "baseUrl/CheckAccount?email=test@test.com&code=123"` 

#### GetRefreshAndAccessToken
- This function uses to get a b2c access token and refresh token from email/password or id token (from b2c login). See [tokens document](https://docs.microsoft.com/en-us/azure/active-directory-b2c/tokens-overview) 
- Http GET
- Parameters:
	+ id_token: the id token from b2c authentication
	+ email: user email
	+ password: user password
- Response:
   + Success (200):
    ```
    {
	    "success": true,
	    "token": {
		    "access_token": "",
		    "refresh_token": "",
		    "expires_on": "",
		    "expires_in" : ""
	    },
	    "group": "new" // B2C user group
	}
    ```
	 + Error 400: 
		 + id_token is invalid or expired
		 + missing email or password
- Example:
	 + `curl "baseUrl/GetRefreshAndAccessToken?email=test@test.com&password=abc`
	 + `curl "baseUrl/GetRefreshAndAccessToken?id_token=123&code=123`	   
	  
#### GetResourceTokens
- This function uses to get the cosmos [resource token](https://docs.microsoft.com/en-us/azure/cosmos-db/secure-access-to-data#resource-tokens-) permissions
- Http GET
- Parameters:
	+ refresh_token: The B2C refresh token from [GetRefreshAndAccessToken](#GetRefreshAndAccessToken)
-  Response:
	+ Success (200)	 
	```
	 {
		"success": true,
		"permissions": [],
		"group": "new", // B2c group
		"refreshToken": "" // the new refresh_token
	 }
	```
    + Error 400: 
	    + refresh_token is invalid
- Example:
	+ `curl baseUrl/GetResourceTokens?refresh_token=abc&code=123`
		
#### GetUserInfo
- Uses to get info from cosmos `User` collection
- Http GET
- Parameters
	+ email: user email
- Response
	+ Success (200):
		```
		{
			"success": true,
			"user": {  // cosmos User table
				"id": "",
				"email": "",
				"firstName": "",
				"lastName": "",
				"type": "",
				// ...
			}
		}
		```		
	
   + Error 400: 
	   + email is missing or invalid 
- Example
	+ `curl baseUrl/GetUserInfo?email=test@test.com&code=123`

#### RefreshToken
- Refresh (renew) the b2c access token and refresh token by current refresh token
- Http GET
- Parameters
	+ refresh_token: current refresh token
- Response:
	+ Success (200):
	```
	{
		"success": true,
		"token": {
		    "access_token": "",
		    "refresh_token": "",
		    "expires_on": "",
		    "expires_in" : ""
	    }
	}
	```
	+ Error 400: 
		+ refresh_token is missing or invalid
- Example:
	+ `curl baseUrl/RefreshToken?refresh_token=abc&code=123`
	
#### CreateRolePermission
- Create Cosmos RolePermission table, use to manage the permission of all other tables in Cosmos. Only role or table parameter can be included in request at a time. And only admin can use this function.
- Http POST
- Parameters:
	+ auth_token: the admin access token 
	+ role: cosmos role (it can be: `read`, `write`, `id-read`, `id-write`)
	+ table: cosmos table name
- Response:
	+ Success (200):
		``` 
		{
			"success": true
		}
		```
		
	+ Error (400):
		+ Both role and table are missing
		+ Both role and table are available in request
- Example:
	+ `curl POST baseUrl/CreateRolePermission?auth_token=abc&role=editor&code=123`
	+ `curl POST baseUrl/CreateRolePermission?auth_token=abc&table=Profile&code=123`
	 
#### UpdateRole
- Update user role (group) in B2C. It also removes all the existing roles of user before assign to new role. Only admin can use this function.
- Http POST
- Parameters:
	+ auth_token: the admin access token
	+ refresh_token: the admin refresh_token
	+ email: user email to update role
	+ role: role (B2C group) to update
- Response:
	+ Success (200):
	```
	{
		"success": true
	}
	``` 
	+ Error (400):		 
		 + email is missing or invalid
		 + role is invalid
	+ Error (401):
		 + refresh_token is invalid
		 + auth_token is invalid 
- Example:
	+ `curl POST baseUrl/UpdateRole?auth_token=abc&email=test@test.com&role=editor&code=123`
	+  `curl POST baseUrl/UpdateRole?refresh_token=abc&email=test@test.com&role=editor&code=123`