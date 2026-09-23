using Core.Chat;

namespace Core.Tests.Chat;

public class ChatHostedAgentResponseStoreTests
{
    [Fact]
    public void GetConversationId_ReturnsNullWhenUnset()
    {
        var store = new ChatHostedAgentResponseStore();

        Assert.Null(store.GetConversationId("Chat3:abc"));
    }

    [Fact]
    public void SetConversationId_RoundTripsPerSession()
    {
        var store = new ChatHostedAgentResponseStore();

        store.SetConversationId("Chat3:one", "conv_1");
        store.SetConversationId("Chat3:two", "conv_2");
        store.SetConversationId("Chat3:one", "conv_1b");

        Assert.Equal("conv_1b", store.GetConversationId("Chat3:one"));
        Assert.Equal("conv_2", store.GetConversationId("Chat3:two"));
    }
}
