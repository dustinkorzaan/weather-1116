using Core.AIWeather.Services;

namespace Core.Tests.AIWeather.Services;

public class AIRunLogRecorderTests
{
    [Fact]
    public void Hydrate_FirstEntry_HasZeroRuntimes()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new AIRunLogRecorder(clock);

        recorder.AddLog(0, "Start", null);
        var entries = recorder.Hydrate();

        Assert.Equal(0, entries[0].RuntimeMs);
        Assert.Equal(0, entries[0].LoopRuntimeMs);
        Assert.Equal(0, entries[0].RunningTotalMs);
        Assert.Equal(0, entries[0].RunningTotalTokenCount);
    }

    [Fact]
    public void Hydrate_NullResponses_AccumulatesZeroRunningTotalTokenCount()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new AIRunLogRecorder(clock);

        recorder.AddLog(0, "Start handler", null);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        recorder.AddLog(1, "Start loop 1", null);

        var entries = recorder.Hydrate();

        Assert.Equal(0, entries[0].RunningTotalTokenCount);
        Assert.Equal(0, entries[1].RunningTotalTokenCount);
    }

    [Fact]
    public void Hydrate_SameLoop_AccumulatesLoopRuntimeFromLoopStart()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new AIRunLogRecorder(clock);

        recorder.AddLog(0, "Start handler", null);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        recorder.AddLog(1, "Start loop 1", null);
        clock.Advance(TimeSpan.FromMilliseconds(50));
        recorder.AddLog(1, "Finish loop 1", null);

        var entries = recorder.Hydrate();

        Assert.Equal(100, entries[1].RuntimeMs);
        Assert.Equal(0, entries[1].LoopRuntimeMs);
        Assert.Equal(50, entries[2].RuntimeMs);
        Assert.Equal(50, entries[2].LoopRuntimeMs);
        Assert.Equal(150, entries[2].RunningTotalMs);
    }

    [Fact]
    public void Hydrate_NewLoop_ResetsLoopRuntimeButNotRunningTotal()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new AIRunLogRecorder(clock);

        recorder.AddLog(1, "Start loop 1", null);
        clock.Advance(TimeSpan.FromMilliseconds(200));
        recorder.AddLog(2, "Start loop 2", null);

        var entries = recorder.Hydrate();

        Assert.Equal(200, entries[1].RuntimeMs);
        Assert.Equal(0, entries[1].LoopRuntimeMs);
        Assert.Equal(200, entries[1].RunningTotalMs);
    }

    [Fact]
    public void Hydrate_RevisitedLoopNumber_AnchorsToFirstOccurrence()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var recorder = new AIRunLogRecorder(clock);

        recorder.AddLog(1, "Start loop 1", null);
        clock.Advance(TimeSpan.FromMilliseconds(30));
        recorder.AddLog(2, "Start loop 2", null);
        clock.Advance(TimeSpan.FromMilliseconds(40));
        recorder.AddLog(1, "Revisit loop 1", null);

        var entries = recorder.Hydrate();

        Assert.Equal(70, entries[2].LoopRuntimeMs);
    }

    private sealed class FakeTimeProvider(DateTime startUtc) : TimeProvider
    {
        private DateTimeOffset _now = startUtc;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
