using System.Text.Json;
using Core.Chat.Models;

namespace Core.Tests.Chat;

public class Chat5SendMessageRequestTests
{
    [Fact]
    public void Deserialize_OmittedGateFlags_DefaultToTrue()
    {
        var request = JsonSerializer.Deserialize<Chat5SendMessageRequest>(
            """{"message":"Hi"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.True(request.EnableMaxLengthGate);
        Assert.True(request.EnableRuleInputGate);
        Assert.True(request.EnableLlmInputGate);
        Assert.True(request.EnableSystemPromptGuard);
        Assert.True(request.EnableLlmOutputGate);
    }

    [Fact]
    public void Deserialize_ExplicitFalseGateFlags_AreHonored()
    {
        var request = JsonSerializer.Deserialize<Chat5SendMessageRequest>(
            """{"message":"Hi","enableLlmOutputGate":false,"enableRuleInputGate":false}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.False(request.EnableLlmOutputGate);
        Assert.False(request.EnableRuleInputGate);
        Assert.True(request.EnableMaxLengthGate);
        Assert.True(request.EnableLlmInputGate);
        Assert.True(request.EnableSystemPromptGuard);
    }
}
