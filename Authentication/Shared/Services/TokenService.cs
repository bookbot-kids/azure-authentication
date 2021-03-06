﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Authentication.Shared.Library;
using Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Token helper
    /// This class uses to validate the tokens
    /// </summary>
    public static class TokenService
    {
        /// <summary>
        /// Azure B2C Issuer
        /// </summary>
        private static readonly string ISSUER = $"https://{Configurations.AzureB2C.TenantName}.b2clogin.com/{Configurations.AzureB2C.TenantId}/";


        /// <summary>
        /// Validate b2c token by the custom b2c policy
        /// Each policy has its own issuer, signing keys.. so we need to make sure all information is correct
        /// </summary>
        /// <param name="idToken">id token. This can be decode by JWT</param>
        /// <param name="policy">custom policy name</param>
        /// <returns>claim principal</returns>
        public static async Task<ClaimsPrincipal> ValidateB2CToken(string idToken, string policy)
        {
            try
            {
                var documentRetriever = new HttpDocumentRetriever { RequireHttps = ISSUER.StartsWith("https://", System.StringComparison.Ordinal) };

                // get the custom policy document to validate
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{ISSUER}/v2.0/.well-known/openid-configuration?p={policy}",
                    new OpenIdConnectConfigurationRetriever(),
                    documentRetriever);

                var config = await configurationManager.GetConfigurationAsync(CancellationToken.None);

                // validate information
                var validationParameter = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidateAudience = false,
                    ValidIssuers = new List<string> { $"https://{Configurations.AzureB2C.TenantName}.b2clogin.com/{Configurations.AzureB2C.TenantId}/v2.0/", $"https://login.microsoftonline.com/{Configurations.AzureB2C.TenantId}/v2.0/" },
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = false,
                    ValidateLifetime = true,
                    IssuerSigningKeys = config.SigningKeys
                };

                ClaimsPrincipal result = null;
                var tries = 0;

                // retry in case error
                while (result == null && tries <= 1)
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();

                        // validate the token with above parameters
                        result = handler.ValidateToken(idToken, validationParameter, out var token);
                    }
                    catch (SecurityTokenSignatureKeyNotFoundException)
                    {
                        // This exception is thrown if the signature key of the JWT could not be found.
                        // This could be the case when the issuer changed its signing keys, so we trigger a 
                        // refresh and retry validation.
                        configurationManager.RequestRefresh();
                        tries++;
                    }
                    catch (SecurityTokenException e)
                    {
                        Logger.Log?.LogError(e.Message);
                        return null;
                    }
                }

                // return claim principal result
                return result;
            }
            catch (Exception e)
            {
                Logger.Log?.LogError(e.Message);
                return null;
            }
        }

        /// <summary>
        /// Generate password from email + secret.
        /// Need to add a prefix to by pass the password complexity requirements
        /// </summary>
        /// <param name="email">email adress</param>
        /// <returns>password hash</returns>
        public static string GeneratePassword(string email)
        {
            return Configurations.AzureB2C.PasswordPrefix + (email.ToLower() + Configurations.AzureB2C.PasswordSecretKey).MD5();
        }
    }
}
