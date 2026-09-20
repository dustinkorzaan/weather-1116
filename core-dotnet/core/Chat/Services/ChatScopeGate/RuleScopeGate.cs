using System.Text.RegularExpressions;

namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b gate #2 ("Code Input"). Deterministic, no I/O, no LLM call: a keyword
/// allow-list and a deny-list decide whether the message is in scope. Kept deliberately
/// simple/blunt to demonstrate a rule gate's limits (e.g. it false-positives on legitimate
/// meta-questions like "what tools do you have?").
/// </summary>
public sealed partial class RuleScopeGate : IScopeGate
{
    // Off-topic/injection signals block regardless of anything else in the message.
    [GeneratedRegex(
        "ignore (all|any|the)? ?(previous|above|prior) instructions|" +
        "disregard (your|the) instructions|" +
        "system prompt|you are now|pretend (you|to) ?are|act as|jailbreak|roleplay|" +
        "write (me )?(a|an) (poem|essay|story|song|code|script|program)|" +
        "translate this|summarize this|password|" +
        "\\bsql\\b|\\bhack\\b|\\bexploit\\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DenyListPattern();

    // Requires at least one weather/location signal to be considered in scope.
    [GeneratedRegex(
        "weather|forecast|temperature|climate|rain(y|ing|fall)?|snow(y|ing|fall)?|wind(y|s)?|" +
        "humid(ity)?|storm(y)?|hurricane|tornado|precipitation|sunny|cloudy|degrees?|°|" +
        "\\bhot\\b|\\bcold\\b|\\bwarm\\b|\\bcool\\b|" +
        "location|coordinates?|latitude|longitude|\\bzip ?code\\b|near me|" +
        "\\bcity\\b|\\btown\\b|\\bstate\\b|where is|geocod",
        RegexOptions.IgnoreCase)]
    private static partial Regex WeatherOrLocationPattern();

    public string Name => "Code Input";

    public Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        if (DenyListPattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message matches an off-topic/instruction-override pattern"));
        }

        if (!WeatherOrLocationPattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message does not contain a weather/location keyword"));
        }

        return Task.FromResult(new ChatScopeGateResult(true, null));
    }
}
