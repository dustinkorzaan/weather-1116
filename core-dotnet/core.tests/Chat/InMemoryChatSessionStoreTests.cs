using Core.Chat.Models;
using Core.Chat.Services;

namespace Core.Tests.Chat;

public class InMemoryChatSessionStoreTests
{
    [Fact]
    public void CreateSession_UsesChatKindPrefix()
    {
        var store = new InMemoryChatSessionStore();

        var sessionId = store.CreateSession("Chat1a");

        Assert.StartsWith("Chat1a:", sessionId, StringComparison.Ordinal);
        Assert.Empty(store.GetMessages(sessionId));
    }

    [Fact]
    public void AppendMessage_PersistsHistoryForSession()
    {
        var store = new InMemoryChatSessionStore();
        var sessionId = store.CreateSession("Chat2a");

        store.AppendMessage(sessionId, new ChatMessage { Role = "user", Content = "Hello" });
        store.AppendMessage(sessionId, new ChatMessage { Role = "assistant", Content = "Hi there" });

        var messages = store.GetMessages(sessionId);
        Assert.Equal(2, messages.Count);
        Assert.Equal("user", messages[0].Role);
        Assert.Equal("assistant", messages[1].Role);
    }
}
