using Core.Chat.Models;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Core.Chat.Services;

/// <summary>
/// Sums token usage across a chat turn's model calls (Responses completed events or
/// Agent Framework <see cref="UsageContent"/>) and records elapsed time from construction
/// to <see cref="ToChatUsage"/>.
/// </summary>
public sealed class ChatUsageAccumulator
{
    private readonly TimeProvider _timeProvider;
    private readonly DateTimeOffset _started;
    private int _input;
    private int _cached;
    private int _output;
    private int _reasoning;
    private int _total;
    private bool _hasUsage;

    public ChatUsageAccumulator(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _started = _timeProvider.GetUtcNow();
    }

    public void Add(StreamingResponseUpdate? update)
    {
        if (update is StreamingResponseCompletedUpdate completed)
        {
            Add(completed.Response?.Usage);
        }
    }

    public void Add(AIContent? content)
    {
        if (content is UsageContent usageContent)
        {
            Add(usageContent.Details);
        }
    }

    public void Add(ResponseTokenUsage? usage)
    {
        if (usage is null)
        {
            return;
        }

        _hasUsage = true;
        _input += usage.InputTokenCount;
        _cached += usage.InputTokenDetails?.CachedTokenCount ?? 0;
        _output += usage.OutputTokenCount;
        _reasoning += usage.OutputTokenDetails?.ReasoningTokenCount ?? 0;
        _total += usage.TotalTokenCount;
    }

    public void Add(UsageDetails? details)
    {
        if (details is null)
        {
            return;
        }

        if (details.InputTokenCount is null
            && details.OutputTokenCount is null
            && details.TotalTokenCount is null)
        {
            return;
        }

        _hasUsage = true;
        _input += ToCount(details.InputTokenCount);
        _cached += ToCount(details.CachedInputTokenCount);
        _output += ToCount(details.OutputTokenCount);
        _reasoning += ToCount(details.ReasoningTokenCount);
        _total += ToCount(details.TotalTokenCount);
    }

    public ChatUsage ToChatUsage() => new()
    {
        InputTokenCount = _hasUsage ? _input : null,
        CachedTokenCount = _hasUsage ? _cached : null,
        OutputTokenCount = _hasUsage ? _output : null,
        ReasoningTokenCount = _hasUsage ? _reasoning : null,
        TotalTokenCount = _hasUsage ? _total : null,
        RuntimeMs = Math.Max(0, (int)(_timeProvider.GetUtcNow() - _started).TotalMilliseconds),
    };

    private static int ToCount(long? value) =>
        value is null ? 0 : (int)Math.Clamp(value.Value, 0, int.MaxValue);
}
