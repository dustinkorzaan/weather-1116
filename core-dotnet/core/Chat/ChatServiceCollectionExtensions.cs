using Core.Chat.Chat1a;
using Core.Chat.Chat1b;
using Core.Chat.Chat2a;
using Core.Chat.Chat2b;
using Core.Chat.Chat3;
using Core.Chat.Chat4a;
using Core.Chat.Chat4b;
using Core.Chat.Chat5a;
using Core.Chat.Chat5b;
using Core.Chat.Services;
using Core.Chat.Services.ChatScopeGate;
using Core.Data.Domain;
using CQMediator;
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

        // Each registration wraps the real service in AgentActivityLoggingChatClientService so
        // every prompt/response for every tab lands in dbo.AgentActivity with no per-controller
        // or per-service changes. FeatureCategory mirrors docs/5-chat-clients/5-chat-clients.md's
        // "Stack" column. "feature" below is only the keyed-DI lookup key controllers/views use
        // (unchanged); the Feature value actually logged is TService's own type name, so it can't
        // drift from the class doing the work.
        AddLoggedChatClient<Chat1aService>(services, "Chat1a", AgentActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat1bService>(services, "Chat1b", AgentActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat2aService>(services, "Chat2a", AgentActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat2bService>(services, "Chat2b", AgentActivityFeatureCategory.ModelDirect);
        AddLoggedChatClient<Chat3Service>(services, "Chat3", AgentActivityFeatureCategory.Agent);
        AddLoggedChatClient<Chat4aService>(services, "Chat4a", AgentActivityFeatureCategory.MultiAgent);
        AddLoggedChatClient<Chat4bService>(services, "Chat4b", AgentActivityFeatureCategory.MultiAgent);

        services.AddSingleton<MaxLengthScopeGate>();
        services.AddSingleton<RuleScopeGate>();
        services.AddKeyedSingleton<IScopeGate>(
            "Chat5InputLlmGate",
            (sp, _) => new LlmScopeGate(sp.GetRequiredService<ChatFoundrySettings>(), "LLM Input"));
        services.AddKeyedSingleton<IScopeGate>(
            "Chat5OutputLlmGate",
            (sp, _) => new LlmScopeGate(sp.GetRequiredService<ChatFoundrySettings>(), "LLM Output"));

        services.AddKeyedScoped<IChat5ClientService, Chat5aService>("Chat5a");
        services.AddKeyedScoped<IChat5ClientService, Chat5bService>("Chat5b");

        return services;
    }

    private static void AddLoggedChatClient<TService>(IServiceCollection services, string feature, string featureCategory)
        where TService : class, IChatClientService
    {
        services.AddKeyedScoped<IChatClientService>(feature, (sp, _) => new AgentActivityLoggingChatClientService(
            ActivatorUtilities.CreateInstance<TService>(sp),
            sp.GetRequiredService<IMediator>(),
            typeof(TService).Name,
            featureCategory));
    }
}
