using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Core.Chat.Services;

/// <summary>
/// Pretty-prints tool call arguments and results for chat UI hover details.
/// </summary>
public static class ChatToolPayload
{
    private static readonly JsonSerializerOptions Pretty = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string? Format(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return FormatText(text);
            case BinaryData binary:
                return FormatText(binary.ToString());
            case JsonElement element:
                return FormatText(element.GetRawText());
            case IEnumerable<AIContent> contents:
                return FormatContents(contents);
            default:
                try
                {
                    return JsonSerializer.Serialize(value, Pretty);
                }
                catch (NotSupportedException)
                {
                    var fallback = value.ToString();
                    return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
                }
        }
    }

    private static string? FormatContents(IEnumerable<AIContent> contents)
    {
        var texts = contents
            .OfType<TextContent>()
            .Select(content => content.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        if (texts.Count == 1)
        {
            return FormatText(texts[0]);
        }

        if (texts.Count > 1)
        {
            return FormatText(JsonSerializer.Serialize(texts, Pretty));
        }

        try
        {
            return JsonSerializer.Serialize(contents, Pretty);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? FormatText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            return JsonSerializer.Serialize(document.RootElement, Pretty);
        }
        catch (JsonException)
        {
            return trimmed;
        }
    }
}
