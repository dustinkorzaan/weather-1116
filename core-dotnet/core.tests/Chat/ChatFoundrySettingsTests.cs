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
            Assert.Equal("Missing AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME.", ex.Message);
        });
    }

    [Fact]
    public void ChatAgentName_UsesEnvironmentValue()
    {
        RunWithFoundryEnvironment(chatAgentName: "wx1116-agent-chat", () =>
        {
            var settings = new ChatFoundrySettings();

            Assert.Equal("wx1116-agent-chat", settings.ChatAgentName);
        });
    }

    private static void RunWithFoundryEnvironment(string? chatAgentName, Action action)
    {
        var previousUrl = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL");
        var previousKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY");
        var previousModel = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL");
        var previousAgent = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME");
        try
        {
            Environment.SetEnvironmentVariable(
                "AZURE_FOUNDRY_PROD_EUS2_PROJ_URL",
                "https://example.services.ai.azure.com/api/projects/demo");
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY", "test-key");
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL", "gpt-5.4-mini");
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME", chatAgentName);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL", previousUrl);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY", previousKey);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL", previousModel);
            Environment.SetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME", previousAgent);
        }
    }
}
