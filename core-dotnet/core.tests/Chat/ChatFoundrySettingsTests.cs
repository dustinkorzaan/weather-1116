using Core.Chat.Services;

namespace Core.Tests.Chat;

public class ChatFoundrySettingsTests
{
    [Fact]
    public void ChatAgentName_DefaultsWhenUnset()
    {
        RunWithFoundryEnvironment(chatAgentName: null, () =>
        {
            var settings = new ChatFoundrySettings();
            Assert.Equal(ChatFoundrySettings.DefaultChatAgentName, settings.ChatAgentName);
        });
    }

    [Fact]
    public void ChatAgentName_DefaultsWhenBlank()
    {
        RunWithFoundryEnvironment(chatAgentName: "  ", () =>
        {
            var settings = new ChatFoundrySettings();
            Assert.Equal(ChatFoundrySettings.DefaultChatAgentName, settings.ChatAgentName);
        });
    }

    [Fact]
    public void ChatAgentName_UsesEnvironmentValue()
    {
        RunWithFoundryEnvironment(chatAgentName: "custom-chat-agent", () =>
        {
            var settings = new ChatFoundrySettings();

            Assert.Equal("custom-chat-agent", settings.ChatAgentName);
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
