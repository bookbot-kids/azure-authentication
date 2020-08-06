using System.Collections.Generic;
using Newtonsoft.Json;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Create AD User parameters
    /// This is serializable class to send on restful
    /// </summary>
    public class CreateADUserParameters
    {
        /// <summary>
        /// Gets or sets a value indicating whether account enabled
        /// </summary>
        [JsonProperty("accountEnabled")]
        public bool AccountEnable { get; set; } = true;

        /// <summary>
        /// Gets or sets createion type
        /// </summary>
        [JsonProperty("creationType")]
        public string CreationType { get; set; } = "LocalAccount";

        /// <summary>
        /// Gets or sets display name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets password profile
        /// </summary>
        [JsonProperty("passwordProfile")]
        public PasswordProfile Profile { get; set; }

        /// <summary>
        /// Gets or sets sign in name
        /// </summary>
        [JsonProperty("signInNames")]
        public List<SignInName> SignInNames { get; set; }

        /// <summary>
        /// The password profile config 
        /// </summary>
        public class PasswordProfile
        {
            /// <summary>
            /// Gets or sets password
            /// </summary>
            [JsonProperty("password")]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether force change password next login
            /// </summary>
            [JsonProperty("forceChangePasswordNextLogin")]
            public bool ForceChangePasswordNextLogin { get; set; } = false;
        }

        /// <summary>
        /// Gets or sets password policies
        /// </summary>
        [JsonProperty("passwordPolicies")]
        public string PasswordPolicies { get; set; } = "DisablePasswordExpiration";

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
    }
}
