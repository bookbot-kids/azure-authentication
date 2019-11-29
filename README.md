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