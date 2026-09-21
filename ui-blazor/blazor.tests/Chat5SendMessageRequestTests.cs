using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class Chat5SendMessageRequestTests
{
    [Fact]
    public void AllGateFlagsDefaultToTrue()
    {
        var request = new Chat5SendMessageRequest { Message = "Hi" };

        Assert.True(request.EnableMaxLengthGate);
        Assert.True(request.EnableRuleInputGate);
        Assert.True(request.EnableLlmInputGate);
        Assert.True(request.EnableSystemPromptGuard);
        Assert.True(request.EnableLlmOutputGate);
    }
}
