using System.Text.Json;

namespace Core.Hangfire;

internal static class HangfireMediatREventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string GetDisplayName(Type eventType) => eventType.Name;

    public static string GetTypeName(Type eventType) => eventType.AssemblyQualifiedName
        ?? throw new InvalidOperationException($"Could not resolve assembly-qualified name for '{eventType.FullName}'.");

    public static string Serialize<TEvent>(TEvent @event)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(@event);
        return JsonSerializer.Serialize(@event, JsonOptions);
    }

    public static object Deserialize(string eventTypeName, string eventJson)
    {
        var eventType = MediatREventTypeResolver.Resolve(eventTypeName);
        return JsonSerializer.Deserialize(eventJson, eventType, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize event '{eventType.Name}'.");
    }
}
