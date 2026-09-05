using Core;
using Core.About;
using Core.Hangfire;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.PostgreSql;
using CQMediator;
using WeatherWorkerDotNet;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStandardCoreServices();
builder.Services.Configure<HangfireAboutHealthOptions>(options =>
	HangfireAboutHealthOptions.Configure(options, builder.Configuration));
builder.Services.AddControllers();

// Durable PostgreSQL storage wherever a connection string is provided
// (DB_CONNECTION_STRING). Falls back to in-memory storage locally so the
// worker still runs without a database.
var dbConnectionString = builder.Configuration["DB_CONNECTION_STRING"];

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
		config.UsePostgreSqlStorage(
			options => options.UseNpgsqlConnection(dbConnectionString),
			new PostgreSqlStorageOptions
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
// does not support. Only register the scheduler when durable PostgreSQL storage is
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
