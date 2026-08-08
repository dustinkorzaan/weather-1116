using Core.Chat.Chat1a;
using Core.Chat.Chat1b;
using Core.Chat.Chat2a;
using Core.Chat.Chat2b;
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
        services.AddScoped<ChatToolExecutor>();

        services.AddScoped<Chat1aService>();
        services.AddScoped<Chat1bService>();
        services.AddScoped<Chat2aService>();
        services.AddScoped<Chat2bService>();

        return services;
    }
}
