using Core.Caching;
using Core.HelloWorld.Handlers;
using Core.Http;
using Core.Tools;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddStandardCoreServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddSingleton<CacheHelper>();
        services.AddSingleton<TransientRetryHelper>();
        services.AddScoped<WeatherToolExecutor>();
        return services;
    }
}
