namespace Core.Chat.Models;

/// <summary>
/// Turn-level token counts and elapsed time, sent on the chat <c>done</c> SSE event.
/// Token fields are null when the model did not report usage; <see cref="RuntimeMs"/> is
/// always the server-side duration of the turn.
/// </summary>
public class ChatUsage
{
    public int? InputTokenCount { get; init; }

    public int? CachedTokenCount { get; init; }

    public int? OutputTokenCount { get; init; }

    public int? ReasoningTokenCount { get; init; }

    public int? TotalTokenCount { get; init; }

    public int RuntimeMs { get; init; }
}
