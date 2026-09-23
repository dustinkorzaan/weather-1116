using Azure.Monitor.OpenTelemetry.Exporter;
using Core;
using Core.Data;
using DotNetEnv;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

// MCP service is stateless with no database. Register a no-op DbContext for handlers that depend on it
// but won't be used by any MCP tools (User handlers are auto-registered but unused here).
builder.Services.AddDbContext<AgentActivityDbContext>((_, options) =>
{
	// Use SqlServer with no connection string - will fail if actually used, but handlers won't be.
	options.UseSqlServer("Server=(local);");
});

builder.Services.AddStandardCoreServices();

builder.Build().Run();
