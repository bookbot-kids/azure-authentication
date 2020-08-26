using Newtonsoft.Json;
using System;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD User Token
    /// This token uses to access b2c api
    /// </summary>
    public class ADToken
    {
        /// <summary>
        /// Gets or sets access token
        /// </summary>
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets refresh token
        /// </summary>
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets resource url
        /// </summary>
        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        /// <summary>
        /// Gets or sets not before
        /// </summary>
        [JsonProperty(PropertyName = "not_before")]
        public string NotBefore { get; set; }

        /// <summary>
        /// Gets or sets expires on
        /// </summary>
        [JsonProperty(PropertyName = "expires_on")]
        public string ExpiresOn { get; set; }

        /// <summary>
        /// Gets or sets expires in
        /// </summary>
        [JsonProperty(PropertyName = "expires_in")]
        public string ExpiresIn { get; set; }

        /// <summary>
        /// Gets a value indicating whether this token is expired
        /// </summary>
        [JsonIgnore]
        public bool IsExpired
        {
            get
            {
                try
                {
                    var expiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(ExpiresOn)).UtcDateTime;
                    return expiration <= DateTime.UtcNow;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
