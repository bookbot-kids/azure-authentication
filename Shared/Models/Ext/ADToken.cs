using System;
using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD Token
    /// This token uses to access b2c api
    /// </summary>
    public partial class ADToken
    {
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
