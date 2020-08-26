﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Authentication.Shared.Models;
using Authentication.Shared.Library;
using Extensions;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Azure directory access helper class.
    /// This class uses to manage all the tokens in Azure directory
    /// </summary>
    public class ADAccess
    {
        /// <summary>
        /// The master token to call b2c api
        /// </summary>
        private ADToken masterToken;

        /// <summary>
        /// Prevents a default instance of the <see cref="ADAccess"/> class from being created
        /// </summary>
        private ADAccess()
        {
        }

        /// <summary>
        /// Gets singleton instance
        /// </summary>
        public static ADAccess Instance { get; } = new ADAccess();

        /// <summary>
        /// Get master key
        /// </summary>
        /// <returns>Master key</returns>
        public async Task<string> GetMasterKey()
        {
            if (masterToken == null || masterToken.IsExpired)
            {
                masterToken = await MicrosoftService.Instance.GetMasterToken();
            }

            return masterToken.AccessToken;
        }

        /// <summary>
        /// Get b2c access token from login with email and password
        /// </summary>
        /// <param name="email">user email</param>
        /// <param name="password">user password</param>
        /// <returns>ADToken class</returns>
        public async Task<ADToken> GetAccessToken(string email, string password = null)
        {
            if (password == null)
            {
                password = TokenHelper.GeneratePassword(email);
            }

            try
            {
                return await AzureB2CService.Instance.GetB2CAccessToken(email, password);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Refresh to get new access token
        /// </summary>
        /// <param name="token">A refresh token</param>
        /// <returns>New access token</returns>
        public async Task<ADToken> RefreshToken(string token)
        {
            try
            {
                return await AzureB2CService.Instance.RefreshB2CToken(token);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Validate mobile client token
        /// </summary>
        /// <param name="token">client token</param>
        /// <returns>Result, message, payload</returns>
        public (bool, string, IDictionary<string, object> payload) ValidateClientToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "token is missing", null);        
            }

            return TokenHelper.ValidateClientToken(token, Configurations.JWTToken.TokenClientSecret.ToBase64(), Configurations.JWTToken.TokenIssuer, Configurations.JWTToken.TokenSubject);
        }

        /// <summary>
        /// Validate B2C Id token
        /// </summary>
        /// <param name="token">Id token</param>
        /// <returns>Result, message, email</returns>
        public async Task<(bool, string, string)> ValidateIdToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "token is missing", null);
            }

            var claimsIdentity = await TokenHelper.ValidateB2CToken(token, Configurations.AzureB2C.SignInSignUpPolicy);
            if (claimsIdentity != null)
            {
                var emailClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("email"));
                if (emailClaim != null)
                {
                    return (true, null, emailClaim.Value);
                }

                return (false, "Can not find user with this token", null);
            }

            return (false, "Token is invalid", null);
        }

        /// <summary>
        /// Validate B2C access token
        /// </summary>
        /// <param name="token">access token</param>
        /// <returns>Result, message, userid</returns>
        public async Task<(bool, string, string)> ValidateAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return (false, "token is missing", null);
            }

            var claimsIdentity = await TokenHelper.ValidateB2CToken(token, Configurations.AzureB2C.AuthPolicy);
            if (claimsIdentity != null)
            {
                var idClaim = claimsIdentity.Claims.FirstOrDefault(c => c.Type.Contains("oid") || c.Type.Contains("objectidentifier"));
                if (idClaim != null)
                {
                    return (true, null, idClaim.Value);
                }

                return (false, "Can not find user with this token", null);
            }

            return (false, "Token is invalid", null);
        }
    }
}
