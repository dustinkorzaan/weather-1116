using Core.about;
using Core.weather.Handlers;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using MediatR;
using WeatherWorkerDotNet;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());

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

builder.Services.AddHostedService<RecurringJobScheduler>();

var app = builder.Build();

// Hangfire dashboard, open to all (POC — no auth). It reads the shared storage,
// so it also shows jobs enqueued by the api/mvc clients.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
	Authorization = [new AllowAllDashboardAuthorizationFilter()],
});

app.MapGet("/about", () =>
{
	var workerNode = AboutTreeBuilder.BuildWorkerDotNetNode();
	var hangfireNode = BuildHangfireNode();
	return Results.Ok(AboutTreeBuilder.BuildWorkerRoot(workerNode, hangfireNode));
});

app.Run();

AboutNode BuildHangfireNode()
{
	try
	{
		var monitoringApi = JobStorage.Current.GetMonitoringApi();
		var statistics = monitoringApi.GetStatistics();
		var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue).Select(item => item.Value).ToList();
		var enqueuedJobs = monitoringApi
			.Queues()
			.SelectMany(queue => monitoringApi.EnqueuedJobs(queue.Name, 0, int.MaxValue))
			.Select(item => item.Value)
			.ToList();

		var now = DateTime.UtcNow;
		var hasStaleProcessing = processingJobs.Any(job =>
			job.StartedAt.HasValue &&
			now - job.StartedAt.Value > TimeSpan.FromMinutes(30));
		var hasStaleEnqueued = enqueuedJobs.Any(job =>
			job.EnqueuedAt.HasValue &&
			now - job.EnqueuedAt.Value > TimeSpan.FromMinutes(60));

		return new AboutNode
		{
			Name = "Hangfire",
			PublicMessage = $"{statistics.Failed} failed, {statistics.Processing} processing, {statistics.Enqueued} enqueued",
			IsHealthy = statistics.Failed == 0 && !hasStaleProcessing && !hasStaleEnqueued,
		};
	}
	catch (Exception exception)
	{
		app.Logger.LogWarning(exception, "Could not read Hangfire monitoring statistics");
		return new AboutNode
		{
			Name = "Hangfire",
			PublicMessage = "Unable to read Hangfire statistics",
			IsHealthy = false,
		};
	}
}
