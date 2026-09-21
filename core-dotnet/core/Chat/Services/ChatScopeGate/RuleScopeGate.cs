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

    // Requires at least one weather signal to be considered in scope. Location words
    // (coordinates, city/town/state, "where is", geocode) are deliberately NOT allow signals on
    // their own: a location is only in scope as part of a weather question about it, not as a
    // topic by itself (e.g. "where is Nashville, TN" is out of scope; "weather in Nashville, TN"
    // is not).
    [GeneratedRegex(
        "weather|forecast|temperature|climate|rain(y|ing|fall)?|snow(y|ing|fall)?|wind(y|s)?|" +
        "humid(ity)?|storm(y)?|hurricane|tornado|precipitation|sunny|cloudy|degrees?|°|" +
        "\\bhot\\b|\\bcold\\b|\\bwarm\\b|\\bcool\\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex WeatherPattern();

    public string Name => "Code Input";

    public Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        if (DenyListPattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message matches an off-topic/instruction-override pattern"));
        }

        if (!WeatherPattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message does not contain a weather keyword"));
        }

        return Task.FromResult(new ChatScopeGateResult(true, null));
    }
}
