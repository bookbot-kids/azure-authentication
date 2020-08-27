using Newtonsoft.Json;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Add an user into group param
    /// This is serializable class to send on restful
    /// </summary>
    public class AddUserToGroupParameter
    {
        /// <summary>
        /// Gets or sets url
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}
