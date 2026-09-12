using Core.Chat.Chat1a;
using Core.Chat.Chat1b;
using Core.Chat.Chat2a;
using Core.Chat.Chat2b;
using Core.Chat.Chat3;
using Core.Chat.Chat4a;
using Core.Chat.Chat4b;
using Core.Chat.Services;
using Core.Data;
using Core.Data.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Chat;

public static class ChatServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherChatClients(this IServiceCollection services)
    {
        services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();
        services.AddSingleton<ChatFoundrySettings>();
        // ChatMcpToolFactory is registered by AddStandardCoreServices (Core needs it for
        // GetCurrentAIWeatherV4Handler in every host, not just chat-client hosts).
        services.AddSingleton<ChatHostedMcpToolFactory>();
        services.AddSingleton<ChatAgentSessionStore>();
        services.AddSingleton<ChatHostedAgentResponseStore>();

        // Each registration wraps the real service in WeatherActivityLoggingChatClientService so
        // every prompt/response for every tab lands in dbo.WeatherActivity with no per-controller
        // or per-service changes. FeatureCategory mirrors docs/5-chat-clients/5-chat-clients.md's
        // "Stack" column.
        AddLoggedChatClient<Chat1aService>(services, "Chat1a", WeatherActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat1bService>(services, "Chat1b", WeatherActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat2aService>(services, "Chat2a", WeatherActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat2bService>(services, "Chat2b", WeatherActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat3Service>(services, "Chat3", WeatherActivityFeatureCategory.Agent);
        AddLoggedChatClient<Chat4aService>(services, "Chat4a", WeatherActivityFeatureCategory.MultiAgent);
        AddLoggedChatClient<Chat4bService>(services, "Chat4b", WeatherActivityFeatureCategory.MultiAgent);

        return services;
    }

    private static void AddLoggedChatClient<TService>(IServiceCollection services, string feature, string featureCategory)
        where TService : class, IChatClientService
    {
        services.AddKeyedScoped<IChatClientService>(feature, (sp, _) => new WeatherActivityLoggingChatClientService(
            ActivatorUtilities.CreateInstance<TService>(sp),
            sp.GetRequiredService<IWeatherActivityLogger>(),
            feature,
            featureCategory));
    }
}
