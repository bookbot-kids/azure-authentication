using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Library
{
    /// <summary>
    /// Logger class
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets logger
        /// </summary>
        public static ILogger Log { get; set; }
    }
}
