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

// In-memory storage keeps the worker self-contained for now. A durable
// backing store (SQL Server / PostgreSQL) will be wired up later.
builder.Services.AddHangfire(config =>
	config.UseMemoryStorage());

builder.Services.AddHangfireServer();

builder.Services.AddHostedService<RecurringJobScheduler>();

var app = builder.Build();

// Always-healthy leaf for now; the API/MVC About trees probe this endpoint.
app.MapGet("/about", () => Results.Ok(AboutTreeBuilder.BuildWorkerDotNetNode(true)));

app.Run();
