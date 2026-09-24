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

// GetCities queries dbo.City (GeoNames cities500, loaded daily by the worker's import-cities job).
// API's Program.cs owns applying EF Core migrations (Database.Migrate()); this app only reads the
// already-migrated schema. Authenticates via this app's user-assigned managed identity
// (AZURE_CLIENT_ID, set by infra/modules/functions-container-app.bicep) -- see
// ManagedIdentitySqlConnectionStringFactory. Without DB_CONNECTION_STRING the host still starts
// (so /about answers) with an unusable placeholder connection; GetCities then fails per call and
// /about reports unhealthy.
var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
	builder.Configuration["DB_CONNECTION_STRING"],
	builder.Configuration["AZURE_CLIENT_ID"]);
builder.Services.AddDbContext<WX1116DbContext>(options =>
	options.UseSqlServer(dbConnectionString ?? "Server=(local);", sql => sql.UseNetTopologySuite()));

builder.Services.AddStandardCoreServices();

builder.Build().Run();
