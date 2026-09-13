using Azure.Monitor.OpenTelemetry.AspNetCore;
using Core;
using Core.About;
using Core.Chat;
using Core.Data;
using Core.Data.Domain;
using Core.Hangfire;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using CQMediator;
using WeatherAPI;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/app-service.bicep). UseAzureMonitor() throws at startup if the
// connection string is missing, so it's opt-in -- local dev and WebApplicationFactory-based
// tests run with no App Insights resource at all.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

// Hangfire client only: this app enqueues jobs onto the shared storage
// (DB_CONNECTION_STRING); the worker is the only app that runs the servers.
// Falls back to in-memory storage locally when no connection string is set.
// Authenticates via this app's user-assigned managed identity (AZURE_CLIENT_ID,
// set by infra/modules/app-service.bicep) instead of a SQL login/password --
// see ManagedIdentitySqlConnectionStringFactory.
var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
	builder.Configuration["DB_CONNECTION_STRING"],
	builder.Configuration["AZURE_CLIENT_ID"]);
builder.Services.AddHangfire(config =>
{
	config.UseDefaultAutomaticRetry();

	if (string.IsNullOrWhiteSpace(dbConnectionString))
	{
		config.UseMemoryStorage();
	}
	else
	{
		// Explicit non-zero poll interval keeps Hangfire on interval polling
		// (every 60s) rather than the aggressive/continuous mode.
		config.UseSqlServerStorage(dbConnectionString, new SqlServerStorageOptions
		{
			QueuePollInterval = TimeSpan.FromSeconds(60),
		});
	}
});

builder.Services.AddControllers();
builder.Services.AddHttpClient<IAboutClient, AboutClient>(client =>
{
	client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddStandardCoreServices();
builder.Services.AddWeatherChatClients();

// dbo.AgentActivity logging (Chat1a-Chat4b and Current AI Weather V3/V4/V5). Unlike Hangfire
// above, DB_CONNECTION_STRING is a hard requirement here -- there is no in-memory fallback, so
// this throws at startup if it's missing. HttpAgentActivityContextProvider lets
// LogAgentActivityHandler capture the inbound request into each row's Context column.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAgentActivityContextProvider, HttpAgentActivityContextProvider>();
builder.Services.AddSingleton<IAgentActivityHostProvider>(new AgentActivityHostProvider(AgentActivityHost.Api));
builder.Services.AddDbContext<AgentActivityDbContext>(options => options.UseSqlServer(dbConnectionString));
builder.Services.AddCors(options =>
{
	options.AddPolicy("ReactClient", policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

		if (allowedOrigins is not { Length: > 0 })
		{
			// No explicit origins configured: fall back to the known local UI dev
			// origins instead of allowing any origin. Configure Cors:AllowedOrigins
			// (appsettings or the Cors__AllowedOrigins__N env vars) for other hosts.
			allowedOrigins = new[]
			{
				"http://localhost:3000",
				"http://localhost:8090",
				"http://localhost:8100",
			};
		}

		policy.WithOrigins(allowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

// Applies any pending EF Core migrations (dbo.AgentActivity and future tables) on every
// startup, so a deploy never needs a separate manual migration step. API is the only host that
// does this -- MVC and the worker also register AgentActivityDbContext, but only read/write the
// schema API has already migrated.
using (var migrationScope = app.Services.CreateScope())
{
	migrationScope.ServiceProvider.GetRequiredService<AgentActivityDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors("ReactClient");

// hack, because the default route is not working in codespaces, so redirect to the about endpoint
app.MapGet("/", () => Results.Redirect("/About"));
app.MapControllers();

app.Run();

public partial class Program;
