using Azure.Monitor.OpenTelemetry.Exporter;
using Core;
using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

Env.TraversePath().Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/function-app.bicep) using the isolated-worker OpenTelemetry pipeline
// (host.json sets telemetryMode: OpenTelemetry). Opt-in because the exporter throws at startup
// if the connection string is missing, and local dev has no App Insights resource.
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
	builder.Logging.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.IncludeScopes = true;
	});

	builder.Services
		.AddOpenTelemetry()
		.UseFunctionsWorkerDefaults()
		.UseAzureMonitorExporter(o => o.ConnectionString = appInsightsConnectionString);
}

builder.Services.AddStandardCoreServices();

builder.Build().Run();
