using Core.AIWeather.Models;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Collects timestamped run-log entries during a GetCurrentAIWeather handler's execution
/// (both V3 and V4), then hydrates elapsed-time and running-total-token fields across the
/// collected entries.
/// </summary>
public class AIRunLogRecorder(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly List<AIRunLogDetail> _entries = [];

    public void AddLog(int loopNumber, string message, ResponseResult? response) => _entries.Add(new AIRunLogDetail
    {
        DateTimeUtc = _timeProvider.GetUtcNow().UtcDateTime,
        LoopNumber = loopNumber,
        Message = message,
        InputTokenCount = response?.Usage?.InputTokenCount,
        CachedTokenCount = response?.Usage?.InputTokenDetails?.CachedTokenCount,
        OutputTokenCount = response?.Usage?.OutputTokenCount,
        ReasoningTokenCount = response?.Usage?.OutputTokenDetails?.ReasoningTokenCount,
        TotalTokenCount = response?.Usage?.TotalTokenCount,
    });

    /// <summary>
    /// Computes RuntimeMs (since the previous entry), LoopRuntimeMs (since the first entry
    /// with the same LoopNumber), RunningTotalMs (since the very first entry), and
    /// RunningTotalTokenCount (cumulative TotalTokenCount) across all collected entries, in
    /// order, and returns the hydrated list.
    /// </summary>
    public List<AIRunLogDetail> Hydrate()
    {
        DateTime? previous = null;
        DateTime? first = null;
        var loopAnchors = new Dictionary<int, DateTime>();
        var runningTotalTokenCount = 0;

        foreach (var entry in _entries)
        {
            first ??= entry.DateTimeUtc;

            entry.RuntimeMs = previous is null
                ? 0
                : (int)(entry.DateTimeUtc - previous.Value).TotalMilliseconds;

            if (!loopAnchors.TryGetValue(entry.LoopNumber, out var anchor))
            {
                anchor = entry.DateTimeUtc;
                loopAnchors[entry.LoopNumber] = anchor;
            }

            entry.LoopRuntimeMs = (int)(entry.DateTimeUtc - anchor).TotalMilliseconds;
            entry.RunningTotalMs = (int)(entry.DateTimeUtc - first.Value).TotalMilliseconds;

            runningTotalTokenCount += entry.TotalTokenCount ?? 0;
            entry.RunningTotalTokenCount = runningTotalTokenCount;

            previous = entry.DateTimeUtc;
        }

        return _entries;
    }
}
