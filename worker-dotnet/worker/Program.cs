using Core.About;
using Core.Weather.Handlers;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using MediatR;
using WeatherWorkerDotNet;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());
builder.Services.Configure<HangfireAboutHealthOptions>(
	_ => HangfireAboutHealthOptions.Bind(builder.Configuration));
builder.Services.AddControllers();

// Durable SQL Server storage wherever a connection string is provided
// (DB_CONNECTION_STRING). Falls back to in-memory storage locally so the
// worker still runs without a database.
var dbConnectionString = builder.Configuration["DB_CONNECTION_STRING"];

// Explicit, non-zero poll interval: a value > TimeSpan.Zero keeps Hangfire on
// interval polling (every 60s) rather than the aggressive/continuous mode.
var queuePollInterval = TimeSpan.FromSeconds(60);

builder.Services.AddHangfire(config =>
{
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

var app = builder.Build();

// Drop legacy recurring job from shared SQL storage (handler removed with forecast demo).
RecurringJob.RemoveIfExists("weather-forecast");

// Hangfire dashboard, open to all (POC — no auth). It reads the shared storage,
// so it also shows jobs enqueued by the api/mvc clients.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = [new AllowAllDashboardAuthorizationFilter()],
});

app.MapControllers();

app.Run();
