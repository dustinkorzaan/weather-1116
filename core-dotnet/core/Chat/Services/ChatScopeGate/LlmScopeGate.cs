using OpenAI.Responses;

namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b gates #3 ("LLM Input") and #5 ("LLM Output"). Issues its own minimal,
/// non-streaming <see cref="ResponsesClient.CreateResponseAsync"/> call — never the
/// orchestration agent itself — classifying whether the given text is about weather (a
/// location by itself does not count).
/// Registered twice under different <see cref="Name"/> values, once for the user's message
/// (gate #3) and once for the orchestrator's completed reply (gate #5).
/// </summary>
public sealed class LlmScopeGate : IScopeGate
{
    private readonly ChatFoundrySettings _settings;

    public LlmScopeGate(ChatFoundrySettings settings, string name)
    {
        _settings = settings;
        Name = name;
    }

    public string Name { get; }

    public async Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        var client = _settings.CreateResponsesClient();
        var options = new CreateResponseOptions
        {
            Model = _settings.DeploymentName,
            Instructions = ChatSystemInstructions.Chat5ScopeClassifierPrompt,
            InputItems = { ResponseItem.CreateUserMessageItem(text) },
        };

        var response = await client.CreateResponseAsync(options, cancellationToken);
        var verdict = response.Value.GetOutputText()?.Trim() ?? string.Empty;

        return new ChatScopeGateResult(
            InScope: verdict.StartsWith("IN_SCOPE", StringComparison.OrdinalIgnoreCase),
            Reason: verdict.Length > 0 ? verdict : "classifier returned no verdict");
    }
}
