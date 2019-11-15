# azure-authentication

## Setup
- Create a file `local.settings.json` in the root project, copy from `sample_local.settings.json` file
- Create b2c tenant https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-create-tenant
- Register b2c application and grant permissions to manage users https://docs.microsoft.com/en-us/azure/active-directory-b2c/tutorial-register-applications?tabs=applications Then fill the `AdminClientId` & `AdminClientSecret` in `local.settings.json` with created application
- Setup custom policy https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-overview-custom. Replace tenant id in sample policies in folder `B2C-policies`
- Setup cosmos SQL database and fill the key into `local.settings.json` https://docs.microsoft.com/en-us/azure/cosmos-db/create-cosmosdb-resources-portal
- Go to Azure b2c portal and create 3 default groups with following name: "new", "admin", "guest" https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal