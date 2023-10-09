using System;
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
using JWT;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Algorithms;
using System.Text;
using System.Security.Cryptography;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Encodings.Web;
using JWT.Serializers;
using System.IO;
using static Authentication.Shared.Configurations;

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
        private static readonly string B2CISSUER = $"https://{Configurations.AzureB2C.TenantName}.b2clogin.com/{Configurations.AzureB2C.TenantId}/";

        /// <summary>
        /// Cognito Issuer
        /// </summary>
        private static readonly string COGNITOISSUER = $"https://cognito-idp.{Configurations.Cognito.CognitoRegion}.amazonaws.com/{Configurations.Cognito.CognitoPoolId}/";

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
                var encodedSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
                var payload = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(encodedSecret)
                    .MustVerifySignature()
                    .WithValidationParameters(ValidationParameters.Default)
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
            catch (SignatureVerificationException e)
            {
                Logger.Log?.LogError(e.Message);
                return (false, "Token has invalid signature", null);
            }
            catch (Exception e)
            {
                Logger.Log?.LogError(e.Message);
                return (false, "Can not validate token, unknown error", null);
            }
        }

        public static async Task<ClaimsPrincipal> ValidateCognitoToken(string idToken)
        {
            try
            {
                var documentRetriever = new HttpDocumentRetriever { RequireHttps = true };

                // get the custom policy document to validate
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{COGNITOISSUER}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever(),
                    documentRetriever);

                var config = await configurationManager.GetConfigurationAsync(CancellationToken.None);

                // validate information
                var validationParameter = new TokenValidationParameters
                {
                    RequireSignedTokens = true,
                    ValidateAudience = false,
                    ValidIssuers = new List<string> { $"https://cognito-idp.{Configurations.Cognito.CognitoRegion}.amazonaws.com/{Configurations.Cognito.CognitoPoolId}" },
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
                var documentRetriever = new HttpDocumentRetriever { RequireHttps = B2CISSUER.StartsWith("https://", System.StringComparison.Ordinal) };

                // get the custom policy document to validate
                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{B2CISSUER}/v2.0/.well-known/openid-configuration?p={policy}",
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

        public static string GenerateAppleToken(string secret, string keyId, string sub, string iss, string aud, DateTime expires)
        {
            ReadOnlySpan<byte> keyAsSpan = Convert.FromBase64String(secret);
            var prvKey = ECDsa.Create();
            prvKey.ImportPkcs8PrivateKey(keyAsSpan, out var read);
            IJwtAlgorithm algorithm = new ES256Algorithm(ECDsa.Create(), prvKey);

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityKey = new ECDsaSecurityKey(prvKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = expires,
                Issuer = iss,
                Audience = aud,
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256),
                Subject = new ClaimsIdentity(new[] { new Claim("sub", sub) }),


            };

            var header = new Dictionary<string, object>()
            {
                { "kid", keyId }
            };


            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            token.Header.Add("kid", keyId);
            return tokenHandler.WriteToken(token);
        }

        public static IDictionary<string, object> DecodeJWTToken(string jwtToken)
        {
            try
            {
                return JwtBuilder.Create()
                     .Decode<IDictionary<string, object>>(jwtToken);
            }
            catch(Exception)
            {
                return new Dictionary<string, object>();
            }
        }

        public static (bool, IDictionary<string, object>) ValidatePublicJWTToken(string jwtToken, IDictionary<string, string> claims)
        {
            try
            {
                var validationParameters = ValidationParameters.None;
                validationParameters.ValidateExpirationTime = true;
                var data = JwtBuilder.Create()
                    .WithDateTimeProvider(new UtcDateTimeProvider())
                    .WithValidationParameters(validationParameters)
                    .WithSerializer(new JsonNetSerializer())
                    .WithUrlEncoder(new JwtBase64UrlEncoder())
                    .Decode<IDictionary<string, object>>(jwtToken);
                foreach(var claim in claims)
                {
                    var value = data.GetOrDefault(claim.Key, "").ToString();
                    if (value != claim.Value)
                    {
                        return (false, null);
                    }
                }

                return (true, data);
            }
            catch (TokenExpiredException)
            {
                Logger.Log?.LogError($"Token {jwtToken} is expired");
                return (false, null);
            }
            catch (Exception ex)
            {
                Logger.Log?.LogError($"Parsing token {jwtToken} error {ex.Message}");
                return (false, null);
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

        public static string EASDecrypt(string key, string iv, string text)
        {
            try
            {
                return EASDecrypt(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(iv), Convert.FromBase64String(text));
            }
            catch(Exception ex)
            {
                Logger.Log?.LogError($"Decrypt AES {text} error {ex.Message}");
                return null;
            }
        }

        public static string EASDecrypt(byte[] key, byte[] iv, byte[] cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var msDecrypt = new MemoryStream(cipherText))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        public static bool IsValidBase64(string text)
        {
            foreach (char c in text)
            {
                if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '+' || c == '/' || c == '=')
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
