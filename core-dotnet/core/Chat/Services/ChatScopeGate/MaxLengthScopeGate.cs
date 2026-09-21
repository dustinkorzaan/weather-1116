namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b gate #1 ("500 Char"). Deterministic, no I/O: blocks a message over 500
/// characters. Cheapest gate in the pipeline, so it runs first when enabled.
/// </summary>
public sealed class MaxLengthScopeGate : IScopeGate
{
    public const int MaxLength = 500;

    public string Name => "500 Char";

    public Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(text.Length <= MaxLength
            ? new ChatScopeGateResult(true, null)
            : new ChatScopeGateResult(false, "message exceeds 500 characters"));
}
