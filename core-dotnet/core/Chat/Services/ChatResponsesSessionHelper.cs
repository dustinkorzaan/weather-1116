using Core.Chat.Models;
using OpenAI.Responses;

namespace Core.Chat.Services;

internal static class ChatResponsesSessionHelper
{
    public const string Chat1aKind = "Chat1a";
    public const string Chat1bKind = "Chat1b";
    public const string Chat2aKind = "Chat2a";
    public const string Chat2bKind = "Chat2b";

    public static string ResolveSessionId(IChatSessionStore sessionStore, string chatKind, string? requestedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId)
            && requestedSessionId.StartsWith($"{chatKind}:", StringComparison.Ordinal)
            && sessionStore.SessionExists(requestedSessionId))
        {
            return requestedSessionId;
        }

        return sessionStore.CreateSession(chatKind);
    }

    public static List<ResponseItem> BuildInputItems(IReadOnlyList<ChatMessage> history, string userMessage)
    {
        var inputItems = new List<ResponseItem>
        {
            ResponseItem.CreateSystemMessageItem(ChatSystemInstructions.WeatherAssistant),
        };

        foreach (var message in history)
        {
            if (string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                inputItems.Add(ResponseItem.CreateUserMessageItem(message.Content));
            }
            else if (string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                inputItems.Add(ResponseItem.CreateAssistantMessageItem(message.Content));
            }
        }

        inputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));
        return inputItems;
    }
}
