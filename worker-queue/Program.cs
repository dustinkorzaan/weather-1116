using Core.weather.Handlers;
using DotNetEnv;
using MediatR;
using WeatherWorkerQueue;

Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetPublicWeatherDataHandler>());

builder.Services.Configure<QueueOptions>(
	builder.Configuration.GetSection(QueueOptions.SectionName));

builder.Services.AddHostedService<QueueWorker>();

var host = builder.Build();

host.Run();
