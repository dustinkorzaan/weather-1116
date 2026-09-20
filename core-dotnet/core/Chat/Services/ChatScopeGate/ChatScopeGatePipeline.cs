namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Runs a list of enabled gates in order with AND semantics: the first to fail blocks and its
/// result is formatted as the message shown to the user. Shared by Chat5aService and
/// Chat5bService so the pipeline logic is defined once and independently testable.
/// </summary>
internal static class ChatScopeGatePipeline
{
    public static async Task<string?> RunAsync(
        IEnumerable<IScopeGate> gates,
        string text,
        CancellationToken cancellationToken)
    {
        foreach (var gate in gates)
        {
            var result = await gate.EvaluateAsync(text, cancellationToken);
            if (!result.InScope)
            {
                return $"Blocked by {gate.Name}: {result.Reason ?? "message is out of scope"}";
            }
        }

        return null;
    }
}
