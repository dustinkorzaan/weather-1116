namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b guardrail gate. A gate classifies a single piece of text (the user's
/// message for input gates, the orchestrator's reply for the output gate) as in scope or not.
/// </summary>
public interface IScopeGate
{
    /// <summary>Matches the checkbox label shown in the UI, e.g. "500 Char", "Code Input".</summary>
    string Name { get; }

    Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken);
}
