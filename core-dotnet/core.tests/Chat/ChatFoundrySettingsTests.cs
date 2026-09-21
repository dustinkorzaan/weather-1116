using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatFoundrySettingsTests
{
    [Fact]
    public void ChatAgentName_ThrowsWhenUnset()
    {
        RunWithFoundryEnvironment(chatAgentName: null, () =>
        {
            var settings = new ChatFoundrySettings();
            var ex = Assert.Throws<InvalidOperationException>(() => settings.ChatAgentName);
            Assert.Equal("Missing AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME.", ex.Message);
        });
    }

    [Fact]
    public void ChatAgentName_UsesEnvironmentValue()
    {
        RunWithFoundryEnvironment(chatAgentName: "wx1116-agent-for-chat", () =>
        {
            var settings = new ChatFoundrySettings();

            Assert.Equal("wx1116-agent-for-chat", settings.ChatAgentName);
        });
    }

    [Fact]
    public void CreateProjectResponsesClientForChatAgent_UsesSharedFoundryAgentFactory()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("core-dotnet/core/Chat/Services/ChatFoundrySettings.cs"));

        Assert.Contains("FoundryAgentResponsesClientFactory.CreateForAgentAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateResponsesClient_UsesManagedIdentityWhenApiKeyUnset()
    {
        RunWithFoundryEnvironment(chatAgentName: null, hasApiKey: false, () =>
        {
            var settings = new ChatFoundrySettings();

            Assert.Null(settings.ApiKey);
            var client = settings.CreateResponsesClient();

            Assert.NotNull(client);
        });
    }

    private static void RunWithFoundryEnvironment(string? chatAgentName, Action action) =>
        RunWithFoundryEnvironment(chatAgentName, hasApiKey: true, action);

    private static void RunWithFoundryEnvironment(string? chatAgentName, bool hasApiKey, Action action)
    {
        var previousUrl = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL");
        var previousKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY");
        var previousModel = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_MODEL");
        var previousAgent = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME");
        try
        {
            Environment.SetEnvironmentVariable(
                "AZURE_FOUNDRY_PROD_PROJ_URL",
                "https://example.services.ai.azure.com/api/projects/demo");
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY", hasApiKey ? "test-key" : null);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_MODEL", "gpt-5.4-mini");
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME", chatAgentName);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL", previousUrl);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY", previousKey);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_MODEL", previousModel);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME", previousAgent);
        }
    }
}
