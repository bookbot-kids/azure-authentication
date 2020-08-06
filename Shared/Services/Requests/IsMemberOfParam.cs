using Newtonsoft.Json;

namespace Authentication.Shared.Services
{
    /// <summary>
    /// Is member of parameter
    /// This is serializable class to send on restful
    /// </summary>
    public class IsMemberOfParam
    {
        /// <summary>
        /// Gets or sets group id
        /// </summary>
        [JsonProperty("groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets member id
        /// </summary>
        [JsonProperty("memberId")]
        public string MemeberId { get; set; }
    }
}
