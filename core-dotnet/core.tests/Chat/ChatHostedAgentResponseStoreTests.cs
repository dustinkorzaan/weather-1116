using Core.Chat;

namespace Core.Tests.Chat;

public class ChatHostedAgentResponseStoreTests
{
    [Fact]
    public void GetPreviousResponseId_ReturnsNullWhenUnset()
    {
        var store = new ChatHostedAgentResponseStore();

        Assert.Null(store.GetPreviousResponseId("Chat3:abc"));
    }

    [Fact]
    public void SetPreviousResponseId_RoundTripsPerSession()
    {
        var store = new ChatHostedAgentResponseStore();

        store.SetPreviousResponseId("Chat3:one", "resp_1");
        store.SetPreviousResponseId("Chat3:two", "resp_2");
        store.SetPreviousResponseId("Chat3:one", "resp_1b");

        Assert.Equal("resp_1b", store.GetPreviousResponseId("Chat3:one"));
        Assert.Equal("resp_2", store.GetPreviousResponseId("Chat3:two"));
    }
}
