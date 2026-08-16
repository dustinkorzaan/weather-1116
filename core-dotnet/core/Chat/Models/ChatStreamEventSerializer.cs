using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Chat.Models;

/// <summary>
/// Serializes SSE payloads with web (camelCase) defaults so browser clients can
/// read the event properties.
/// </summary>
public static class ChatStreamEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(ChatStreamEvent streamEvent) => JsonSerializer.Serialize(streamEvent, Options);
}
