namespace WeatherBlazor.Data;

// Chat5a/Chat5b gate toggles, shared by ChatPanel (/chat-clients, all five on) and the Home
// Chat5aSidebar (Code Input off by default). Rendered by Chat5GateOptions.
public sealed class Chat5GateState
{
    public bool MaxLength { get; set; } = true;
    public bool RuleInput { get; set; } = true;
    public bool LlmInput { get; set; } = true;
    public bool SystemPrompt { get; set; } = true;
    public bool LlmOutput { get; set; } = true;
}
