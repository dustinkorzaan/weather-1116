using Core.Chat.Chat1a;
using Core.Chat.Chat1b;
using Core.Chat.Chat2a;
using Core.Chat.Chat2b;
using Core.Chat.Chat3;
using Core.Chat.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Chat;

public static class ChatServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherChatClients(this IServiceCollection services)
    {
        services.AddSingleton<IChatSessionStore, InMemoryChatSessionStore>();
        services.AddSingleton<ChatFoundrySettings>();
        services.AddSingleton<ChatMcpToolFactory>();
        services.AddSingleton<ChatHostedMcpToolFactory>();
        services.AddSingleton<ChatAgentSessionStore>();
        services.AddSingleton<ChatHostedAgentResponseStore>();

        services.AddKeyedScoped<IChatClientService, Chat1aService>("Chat1a");
        services.AddKeyedScoped<IChatClientService, Chat1bService>("Chat1b");
        services.AddKeyedScoped<IChatClientService, Chat2aService>("Chat2a");
        services.AddKeyedScoped<IChatClientService, Chat2bService>("Chat2b");
        services.AddKeyedScoped<IChatClientService, Chat3Service>("Chat3");

        return services;
    }
}
