using Core.Chat.Services;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Core.Tests.Chat;

public class ChatUsageAccumulatorTests
{
    [Fact]
    public void ToChatUsage_NoModelUsage_HasRuntimeOnly()
    {
        var clock = new FakeTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var accumulator = new ChatUsageAccumulator(clock);
        clock.Advance(TimeSpan.FromMilliseconds(1250));

        var usage = accumulator.ToChatUsage();

        Assert.Null(usage.InputTokenCount);
        Assert.Null(usage.CachedTokenCount);
        Assert.Null(usage.OutputTokenCount);
        Assert.Null(usage.ReasoningTokenCount);
        Assert.Null(usage.TotalTokenCount);
        Assert.Equal(1250, usage.RuntimeMs);
    }

    [Fact]
    public void Add_UsageDetails_SumsAcrossCalls()
    {
        var accumulator = new ChatUsageAccumulator();
        accumulator.Add(new UsageDetails
        {
            InputTokenCount = 10,
            CachedInputTokenCount = 2,
            OutputTokenCount = 4,
            ReasoningTokenCount = 1,
            TotalTokenCount = 14,
        });
        accumulator.Add(new UsageDetails
        {
            InputTokenCount = 20,
            CachedInputTokenCount = 5,
            OutputTokenCount = 6,
            ReasoningTokenCount = 2,
            TotalTokenCount = 26,
        });

        var usage = accumulator.ToChatUsage();

        Assert.Equal(30, usage.InputTokenCount);
        Assert.Equal(7, usage.CachedTokenCount);
        Assert.Equal(10, usage.OutputTokenCount);
        Assert.Equal(3, usage.ReasoningTokenCount);
        Assert.Equal(40, usage.TotalTokenCount);
    }

    [Fact]
    public void Add_NullResponseTokenUsage_DoesNotMarkHasUsage()
    {
        var accumulator = new ChatUsageAccumulator();
        accumulator.Add((ResponseTokenUsage?)null);

        var usage = accumulator.ToChatUsage();

        Assert.Null(usage.TotalTokenCount);
    }

    [Fact]
    public void Add_UsageContent_ExtractsDetails()
    {
        var accumulator = new ChatUsageAccumulator();
        accumulator.Add(new UsageContent(new UsageDetails
        {
            InputTokenCount = 8,
            OutputTokenCount = 2,
            TotalTokenCount = 10,
        }));

        var usage = accumulator.ToChatUsage();

        Assert.Equal(8, usage.InputTokenCount);
        Assert.Equal(2, usage.OutputTokenCount);
        Assert.Equal(10, usage.TotalTokenCount);
        Assert.Equal(0, usage.CachedTokenCount);
        Assert.Equal(0, usage.ReasoningTokenCount);
    }

    [Fact]
    public void Add_EmptyUsageDetails_DoesNotMarkHasUsage()
    {
        var accumulator = new ChatUsageAccumulator();
        accumulator.Add(new UsageDetails());

        Assert.Null(accumulator.ToChatUsage().TotalTokenCount);
    }

    private sealed class FakeTimeProvider(DateTime startUtc) : TimeProvider
    {
        private DateTimeOffset _now = startUtc;

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
