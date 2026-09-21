namespace Core.Chat.Models;

/// <summary>
/// Chat5a/Chat5b only. Sibling of <see cref="ChatSendMessageRequest"/> (not a modification of
/// it) so Chat1-4's contract stays frozen. Carries the five guardrail-gate toggles shown as
/// checkboxes below the chat input, all defaulting to checked/true.
/// </summary>
public class Chat5SendMessageRequest
{
    public string? SessionId { get; set; }

    public required string Message { get; set; }

    // Gate #1: 500 Char.
    public bool EnableMaxLengthGate { get; set; } = true;

    // Gate #2: Code Input.
    public bool EnableRuleInputGate { get; set; } = true;

    // Gate #3: LLM Input.
    public bool EnableLlmInputGate { get; set; } = true;

    // Gate #4: Sys Prompt.
    public bool EnableSystemPromptGuard { get; set; } = true;

    // Gate #5: LLM Output.
    public bool EnableLlmOutputGate { get; set; } = true;
}
