using Core.weather.Handlers;
using DotNetEnv;
using MediatR;
using WeatherWorkerQueue;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());

builder.Services.Configure<QueueOptions>(options =>
{
	options.ConnectionString = builder.Configuration["DB_CONNECTION_STRING"];
	options.RequestQueueName =
		builder.Configuration["WEATHER_REQUEST_QUEUE_NAME"] ?? options.RequestQueueName;
	options.ResponseQueueName =
		builder.Configuration["WEATHER_RESPONSE_QUEUE_NAME"] ?? options.ResponseQueueName;
});

builder.Services.AddHostedService<QueueWorker>();

var host = builder.Build();

host.Run();
