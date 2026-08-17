using Core.Caching;
using Core.Geo.Handlers;
using Core.Http;
using DotNetEnv;
using MediatR;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Env.TraversePath().Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetLatLongHandler>());
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<CacheHelper>();
builder.Services.AddSingleton<TransientRetryHelper>();

builder.Build().Run();
