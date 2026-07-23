using Core.weather.Handlers;
using DotNetEnv;
using Hangfire;
using Hangfire.MemoryStorage;
using MediatR;
using WeatherWorkerDotNet;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());

// In-memory storage keeps the worker self-contained for now. A durable
// backing store (SQL Server / PostgreSQL) will be wired up later.
builder.Services.AddHangfire(config =>
	config.UseMemoryStorage());

builder.Services.AddHangfireServer();

builder.Services.AddHostedService<RecurringJobScheduler>();

var host = builder.Build();

host.Run();
