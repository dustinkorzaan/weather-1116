namespace Core.Chat.Services.ChatScopeGate;

public sealed record ChatScopeGateResult(bool InScope, string? Reason);
