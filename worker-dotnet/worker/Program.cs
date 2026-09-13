using Azure.Monitor.OpenTelemetry.AspNetCore;
using Core;
using Core.About;
using Core.Data;
using Core.Data.Domain;
using Core.Hangfire;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using CQMediator;
using WeatherWorkerDotNet;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
// (set by infra/modules/app-service.bicep). UseAzureMonitor() throws at startup if the
// connection string is missing, so it's opt-in -- local dev runs with no App Insights resource.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
	builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddStandardCoreServices();
builder.Services.Configure<HangfireAboutHealthOptions>(options =>
	HangfireAboutHealthOptions.Configure(options, builder.Configuration));
builder.Services.AddControllers();

// dbo.AgentActivity logging: the confirm-nashville-ai-weather-v3/v4 recurring jobs below call
// straight into GetCurrentAIWeatherV3Handler/V4Handler, the same handlers API's AIWeatherController
// and MVC's HomeController call -- this is the third host (alongside Api and Mvc) that can produce
// AgentActivity rows. DB_CONNECTION_STRING is a hard requirement here, same as API and MVC.
// HttpAgentActivityContextProvider is registered too, but a Hangfire recurring job has no
// ambient HttpContext, so it always returns null and rows logged from this host leave Context null.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAgentActivityContextProvider, HttpAgentActivityContextProvider>();
builder.Services.AddSingleton<IAgentActivityHostProvider>(new AgentActivityHostProvider(AgentActivityHost.Worker));

// Durable SQL Server storage wherever a connection string is provided
// (DB_CONNECTION_STRING). Falls back to in-memory storage locally so the
// worker still runs without a database. Authenticates via this app's
// user-assigned managed identity (AZURE_CLIENT_ID, set by
// infra/modules/app-service.bicep) instead of a SQL login/password -- see
// ManagedIdentitySqlConnectionStringFactory.
var dbConnectionString = ManagedIdentitySqlConnectionStringFactory.Build(
	builder.Configuration["DB_CONNECTION_STRING"],
	builder.Configuration["AZURE_CLIENT_ID"]);

// Unlike Hangfire's own storage above, there is no in-memory fallback here -- this throws at
// startup if DB_CONNECTION_STRING is missing. API's Program.cs owns applying migrations
// (Database.Migrate()); this app only reads/writes the already-migrated schema.
builder.Services.AddDbContext<AgentActivityDbContext>(options => options.UseSqlServer(dbConnectionString));

// Explicit, non-zero poll interval: a value > TimeSpan.Zero keeps Hangfire on
// interval polling (every 60s) rather than the aggressive/continuous mode.
var queuePollInterval = TimeSpan.FromSeconds(60);

builder.Services.AddHangfire(config =>
{
	config.UseDefaultAutomaticRetry();

	if (string.IsNullOrWhiteSpace(dbConnectionString))
	{
		config.UseMemoryStorage();
	}
	else
	{
		config.UseSqlServerStorage(dbConnectionString, new SqlServerStorageOptions
		{
			QueuePollInterval = queuePollInterval,
		});
	}
});

// The worker is the only app that runs Hangfire servers. It runs one server
// per queue so each queue's concurrency can be tuned independently.
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "default";
	options.Queues = ["default"];
	options.WorkerCount = 1;
	options.SchedulePollingInterval = queuePollInterval;
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "default-single";
	options.Queues = ["default-single"];
	options.WorkerCount = 1;
	options.SchedulePollingInterval = queuePollInterval;
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "batch-single";
	options.Queues = ["batch-single"];
	options.WorkerCount = 1;
	options.SchedulePollingInterval = queuePollInterval;
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "batch-multi";
	options.Queues = ["batch-multi"];
	options.WorkerCount = 10;
	options.SchedulePollingInterval = queuePollInterval;
});

// Recurring jobs use Hangfire's explicit queue overload, which MemoryStorage
// does not support. Only register the scheduler when durable SQL storage is
// configured via DB_CONNECTION_STRING.
if (!string.IsNullOrWhiteSpace(dbConnectionString))
{
	builder.Services.AddHostedService<RecurringJobScheduler>();
}

var app = builder.Build();

// Hangfire dashboard, open to all (POC — no auth). It reads the shared storage,
// so it also shows jobs enqueued by the api/mvc clients.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = [new AllowAllDashboardAuthorizationFilter()],
});

app.MapControllers();

app.Run();
