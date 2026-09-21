using Core.Chat.Services.ChatScopeGate;

namespace Core.Tests.Chat;

public class ChatScopeGatePipelineTests
{
    private sealed class FakeGate : IScopeGate
    {
        public string Name { get; init; } = "Fake";
        public bool InScope { get; init; } = true;
        public string? Reason { get; init; }
        public int CallCount { get; private set; }

        public Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new ChatScopeGateResult(InScope, Reason));
        }
    }

    [Fact]
    public async Task RunAsync_ReturnsNullWhenAllGatesPass()
    {
        var gateA = new FakeGate { Name = "A", InScope = true };
        var gateB = new FakeGate { Name = "B", InScope = true };

        var result = await ChatScopeGatePipeline.RunAsync([gateA, gateB], "hello", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, gateA.CallCount);
        Assert.Equal(1, gateB.CallCount);
    }

    [Fact]
    public async Task RunAsync_BlocksOnFirstFailingGateAndSkipsLaterGates()
    {
        var gateA = new FakeGate { Name = "A", InScope = false, Reason = "nope" };
        var gateB = new FakeGate { Name = "B", InScope = true };

        var result = await ChatScopeGatePipeline.RunAsync([gateA, gateB], "hello", CancellationToken.None);

        Assert.Equal("Blocked by A: nope", result);
        Assert.Equal(1, gateA.CallCount);
        Assert.Equal(0, gateB.CallCount);
    }

    [Fact]
    public async Task RunAsync_ADisabledGateOmittedFromTheListIsNeverInvoked()
    {
        var disabledGate = new FakeGate { Name = "Disabled", InScope = false };
        var enabledGate = new FakeGate { Name = "Enabled", InScope = true };

        // Simulates the caller only adding enabled gates to the list (per its
        // request flags) before handing it to the pipeline.
        var result = await ChatScopeGatePipeline.RunAsync([enabledGate], "hello", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, disabledGate.CallCount);
    }

    [Fact]
    public async Task RunAsync_UsesFallbackReasonWhenGateReturnsNoneOnFailure()
    {
        var gate = new FakeGate { Name = "NoReason", InScope = false, Reason = null };

        var result = await ChatScopeGatePipeline.RunAsync([gate], "hello", CancellationToken.None);

        Assert.Equal("Blocked by NoReason: message is out of scope", result);
    }
}
