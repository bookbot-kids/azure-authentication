using Authentication.Shared;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;

[assembly: FunctionsStartup(typeof(Authentication.Startup))]

namespace Authentication
{
    /// <summary>
    /// App function starting up class
    /// This is the first point before all the functions start
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configure the app dependencies injection
        /// </summary>
        /// <param name="builder">Function builder to register dependencies injection</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IdentityModelEventSource.ShowPII = true;
            // Set the configuration from local.settings.json into constant class
            builder.Services.AddOptions<object>().Configure<IConfiguration>((settings, configuration) =>
                {
                    Configurations.Configuration = configuration;
                });
        }
    }
}
