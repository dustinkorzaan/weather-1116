using OpenAI.Responses;

namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b gates #3 ("LLM Input") and #5 ("LLM Output"). Issues its own minimal,
/// non-streaming <see cref="ResponsesClient.CreateResponseAsync"/> call — never the
/// orchestration agent itself — classifying whether the given text is about weather (a
/// location by itself does not count).
/// Registered twice under different <see cref="Name"/> values, once for the user's message
/// (gate #3, with <see cref="ChatSystemInstructions.Chat5InputScopeClassifierPrompt"/>) and once
/// for the orchestrator's completed reply (gate #5, with
/// <see cref="ChatSystemInstructions.Chat5OutputScopeClassifierPrompt"/>) — a request is a
/// question and a reply is a statement, so each gets its own classifier prompt rather than one
/// prompt trying to cover both shapes.
/// </summary>
public sealed class LlmScopeGate : IScopeGate
{
    private readonly ChatFoundrySettings _settings;
    private readonly string _classifierPrompt;

    public LlmScopeGate(ChatFoundrySettings settings, string name, string classifierPrompt)
    {
        _settings = settings;
        Name = name;
        _classifierPrompt = classifierPrompt;
    }

    public string Name { get; }

    public async Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        var client = _settings.CreateResponsesClient();
        var options = new CreateResponseOptions
        {
            Model = _settings.DeploymentName,
            Instructions = _classifierPrompt,
            InputItems = { ResponseItem.CreateUserMessageItem(text) },
        };

        var response = await client.CreateResponseAsync(options, cancellationToken);
        var verdict = response.Value.GetOutputText()?.Trim() ?? string.Empty;

        return new ChatScopeGateResult(
            InScope: verdict.StartsWith("IN_SCOPE", StringComparison.OrdinalIgnoreCase),
            Reason: verdict.Length > 0 ? verdict : "classifier returned no verdict");
    }
}
