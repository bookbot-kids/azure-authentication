using System.Collections.Generic;
using Newtonsoft.Json;

namespace Authentication.Shared.Models
{
    /// <summary>
    /// AD user
    /// It contains information of AD user
    /// </summary>
    public partial class ADUser
    {
        /// <summary>
        /// Gets or sets object id
        /// </summary>
        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether account is enabled
        /// </summary>
        [JsonProperty("accountEnabled")]
        public bool AccountEnabled { get; set; }

        /// <summary>
        /// Gets or sets user type
        /// </summary>
        [JsonProperty("userType")]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets sign in name
        /// </summary>
        [JsonProperty("signInNames")]
        public List<SignInName> SignInNames { get; set; }

        /// <summary>
        /// Sign in name config
        /// </summary>
        public class SignInName
        {
            /// <summary>
            /// Gets or sets type
            /// </summary>
            [JsonProperty("type")]
            public string Type { get; set; } = "emailAddress";

            /// <summary>
            /// Gets or sets value
            /// </summary>
            [JsonProperty("value")]
            public string Value { get; set; }
        }

        /// <summary>
        /// Gets or sets password policies
        /// </summary>
        [JsonProperty("passwordPolicies")]
        public string PasswordPolicies { get; set; }
    }
}
