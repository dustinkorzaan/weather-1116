using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatResponsesSessionHelperTests
{
    [Fact]
    public void ResolveSessionId_ReusesSession_WhenKindMatches()
    {
        var store = new InMemoryChatSessionStore();
        var sessionId = store.CreateSession(ChatResponsesSessionHelper.Chat1aKind);

        var resolved = ChatResponsesSessionHelper.ResolveSessionId(
            store,
            ChatResponsesSessionHelper.Chat1aKind,
            sessionId);

        Assert.Equal(sessionId, resolved);
    }

    [Fact]
    public void ResolveSessionId_CreatesNewSession_WhenKindDoesNotMatch()
    {
        var store = new InMemoryChatSessionStore();
        var chat1bSession = store.CreateSession(ChatResponsesSessionHelper.Chat1bKind);

        var resolved = ChatResponsesSessionHelper.ResolveSessionId(
            store,
            ChatResponsesSessionHelper.Chat1aKind,
            chat1bSession);

        Assert.StartsWith($"{ChatResponsesSessionHelper.Chat1aKind}:", resolved, StringComparison.Ordinal);
        Assert.NotEqual(chat1bSession, resolved);
    }

    [Fact]
    public void ResolveSessionId_CreatesNewSession_WhenRequestedIdUnknown()
    {
        var store = new InMemoryChatSessionStore();

        var resolved = ChatResponsesSessionHelper.ResolveSessionId(
            store,
            ChatResponsesSessionHelper.Chat2aKind,
            "Chat2a:does-not-exist");

        Assert.StartsWith($"{ChatResponsesSessionHelper.Chat2aKind}:", resolved, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveSessionId_UsesChat3KindPrefix()
    {
        var store = new InMemoryChatSessionStore();

        var resolved = ChatResponsesSessionHelper.ResolveSessionId(
            store,
            ChatResponsesSessionHelper.Chat3Kind,
            null);

        Assert.StartsWith($"{ChatResponsesSessionHelper.Chat3Kind}:", resolved, StringComparison.Ordinal);
    }
}
