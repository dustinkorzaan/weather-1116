using Core.Chat.Services.ChatScopeGate;

namespace Core.Tests.Chat;

public class MaxLengthScopeGateTests
{
    private readonly MaxLengthScopeGate _gate = new();

    [Fact]
    public async Task EvaluateAsync_AllowsMessageAtOrUnder500Characters()
    {
        var result = await _gate.EvaluateAsync(new string('a', 500), CancellationToken.None);

        Assert.True(result.InScope);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_BlocksMessageOver500Characters()
    {
        var result = await _gate.EvaluateAsync(new string('a', 501), CancellationToken.None);

        Assert.False(result.InScope);
        Assert.Contains("500", result.Reason);
    }

    [Fact]
    public void Name_MatchesCheckboxLabel()
    {
        Assert.Equal("500 Char", _gate.Name);
    }
}
