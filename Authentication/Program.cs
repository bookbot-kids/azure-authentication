using System.Linq;
using Authentication.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Keep the in-process response format: Newtonsoft.Json (camelCase, [JsonProperty] names honored)
builder.Services.AddMvcCore().AddNewtonsoftJson();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// The Application Insights SDK only sends Warning and above by default; keep Information logs like in-process did
builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    var defaultRule = options.Rules.FirstOrDefault(rule =>
        rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }
});

IdentityModelEventSource.ShowPII = true;
// Set the configuration from local.settings.json into constant class
Configurations.Configuration = builder.Configuration;

builder.Build().Run();
