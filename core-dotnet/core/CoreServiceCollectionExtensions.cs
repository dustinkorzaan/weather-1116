using Core.Caching;
using Core.Chat.Services;
using Core.HelloWorld.Handlers;
using Core.Http;
using Core.Tools;
using CQMediator;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddStandardCoreServices(this IServiceCollection services)
    {
        services.AddCQMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<HelloWorldHandler>());
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddSingleton<CacheHelper>();
        services.AddSingleton<TransientRetryHelper>();
        services.AddScoped<WeatherToolExecutor>();
        // GetCurrentAIWeatherV4Handler (registered above via CQMediator assembly scanning) needs
        // this remote-MCP tool factory in every host that includes Core, not just the ones
        // that also call AddWeatherChatClients().
        services.AddSingleton<ChatMcpToolFactory>();
        return services;
    }
}
