using Core.about;
using Core.weather.Handlers;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
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

builder.Services.AddHangfire(config =>
{
	if (string.IsNullOrWhiteSpace(dbConnectionString))
	{
		config.UseMemoryStorage();
	}
	else
	{
		config.UseSqlServerStorage(dbConnectionString);
	}
});

// The worker is the only app that runs Hangfire servers. It runs one server
// per queue so each queue's concurrency can be tuned independently.
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "default";
	options.Queues = ["default"];
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "default-single";
	options.Queues = ["default-single"];
	options.WorkerCount = 1;
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "batch-single";
	options.Queues = ["batch-single"];
	options.WorkerCount = 1;
});
builder.Services.AddHangfireServer(options =>
{
	options.ServerName = "batch-multi";
	options.Queues = ["batch-multi"];
	options.WorkerCount = 10;
});

builder.Services.AddHostedService<RecurringJobScheduler>();

var app = builder.Build();

// Always-healthy leaf for now; the API/MVC About trees probe this endpoint.
app.MapGet("/about", () => Results.Ok(AboutTreeBuilder.BuildWorkerDotNetNode(true)));

app.Run();
