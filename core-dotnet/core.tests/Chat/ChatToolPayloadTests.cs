using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatToolPayloadTests
{
    [Fact]
    public void Format_PrettyPrintsJsonString()
    {
        var formatted = ChatToolPayload.Format("""{"location":"Nashville, TN"}""");

        Assert.Contains("\"location\"", formatted);
        Assert.Contains("Nashville, TN", formatted);
        Assert.Contains('\n', formatted);
    }

    [Fact]
    public void Format_PrettyPrintsDictionaryArguments()
    {
        var formatted = ChatToolPayload.Format(new Dictionary<string, object?>
        {
            ["latitude"] = 36,
            ["longitude"] = -86,
        });

        Assert.Contains("latitude", formatted);
        Assert.Contains("36", formatted);
        Assert.Contains("longitude", formatted);
    }

    [Fact]
    public void Format_ReturnsNullForEmptyValues()
    {
        Assert.Null(ChatToolPayload.Format((string?)null));
        Assert.Null(ChatToolPayload.Format("   "));
        Assert.Null(ChatToolPayload.Format((object?)null));
    }

    [Fact]
    public void Format_LeavesPlainTextUnchanged()
    {
        Assert.Equal("tool failed", ChatToolPayload.Format("tool failed"));
    }
}
