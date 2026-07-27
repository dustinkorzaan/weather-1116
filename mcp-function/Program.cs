using Azure.Core.Serialization;
using Core.geo.Handlers;
using DotNetEnv;
using MediatR;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

Env.TraversePath().Load();

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<Microsoft.Azure.Functions.Worker.WorkerOptions>(options =>
{
	options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	});
});

builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssemblyContaining<GetLatLongDataHandler>());

builder.Build().Run();
