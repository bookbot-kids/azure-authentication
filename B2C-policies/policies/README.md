These are Azure B2C custom policies to custom the authenticate flow https://docs.microsoft.com/en-us/azure/active-directory-b2c/custom-policy-get-started
These policies are taken from [passwordless-email] (https://github.com/azure-ad-b2c/samples/tree/master/policies/passwordless-email) which allow to sign in without password
- ROPC_Auth: The custom policy uses to manage user's tokens (access token and refresh token) by using [Azure AD Graph API] (https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-graph-api)
- SignUpOrSignIn: The custom policy to authenticate user by email on a b2c site
- TrustFrameworkBase: contains most of the definitions like validation, technical, custom authenticate site, css...
- TrustFrameworkExtensions: The extensions from the base, define the authenticate flow, from enter email page to the token page