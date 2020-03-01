using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using JWT;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Authentication.Shared.Utils
{
    /// <summary>
    /// Token helper
    /// This class uses to validate the tokens
    /// </summary>
    public static class TokenHelper
    {
        /// <summary>
        /// Azure B2C Issuer
        /// </summary>
        private static readonly string ISSUER = $"https://{Configurations.AzureB2C.TenantName}.b2clogin.com/{Configurations.AzureB2C.TenantId}/";

        /// <summary>
        /// Validate client token from mobile by checking issuer and subject of token
        /// We use JWT to validate the token
        /// </summary>
        /// <param name="token">token id</param>
        /// <param name="secret">secret key</param>
        /// <param name="iss">issuer param</param>
        /// <param name="sub">subject param</param>
        /// <returns>result, message, payload</returns>
        public static (bool result, string errorMessage, IDictionary<string, object> payload) ValidateClientToken(string token, string secret, string iss, string sub)
        {
            try
            {
                var payload = new JwtBuilder()
                    .WithSecret(secret)
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(token);

                if (iss.Equals(payload["iss"]) && sub.Equals(payload["sub"]))
                {
                    return (true, "success", payload);
                }

                return (false, "Invalid payload", null);
            }
            catch (TokenExpiredException)
            {
                return (false, "Token has expired", null);
            }
            catch (SignatureVerificationException)
            {
                return (false, "Token has invalid signature", null);
            }
        }

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
    }
}
