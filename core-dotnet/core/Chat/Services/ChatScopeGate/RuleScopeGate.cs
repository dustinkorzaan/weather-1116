using System.Text.RegularExpressions;

namespace Core.Chat.Services.ChatScopeGate;

/// <summary>
/// Chat5a/Chat5b gate #2 ("Code Input"). Deterministic, no I/O, no LLM call: a keyword
/// allow-list and a deny-list decide whether the message is in scope. In scope means one of
/// five request kinds: weather, a location/geo question, listing the user's saved cities,
/// adding/saving a city, or removing/deleting a saved city. Kept deliberately simple/blunt to
/// demonstrate a rule gate's limits (e.g. it false-positives on legitimate meta-questions like
/// "what tools do you have?"; a broad verb like "add" or "remove" lets some off-topic text
/// through; and it does not catch an in-scope request with an unrelated request bundled in,
/// since the deny-list is a fixed set of patterns rather than semantic understanding — those
/// cases are caught by the smarter "LLM Input"/"LLM Output" gates instead, see
/// <see cref="LlmScopeGate"/>).
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

    // Requires at least one in-scope signal: a weather word, a location/geo word (where is,
    // coordinates, a numeric lat/long pair, city/cities, near, location), or a saved-city word
    // (add/save/pin/unpin/remove/delete, "my cities"/"my pins"/"my list"/"my locations").
    [GeneratedRegex(
        "weather|forecast|temperature|climate|rain(y|ing|fall)?|snow(y|ing|fall)?|wind(y|s)?|" +
        "humid(ity)?|storm(y)?|hurricane|tornado|precipitation|sunny|cloudy|degrees?|°|" +
        "\\bhot\\b|\\bcold\\b|\\bwarm\\b|\\bcool\\b|" +
        "\\bwhere is\\b|\\bcoordinates?\\b|\\blat(itude)?\\b|\\blongitude\\b|\\blocations?\\b|" +
        "-?\\d+(\\.\\d+)?\\s*,\\s*-?\\d+(\\.\\d+)?|\\bcit(y|ies)\\b|\\bnear(by|est)?\\b|" +
        "\\b(add|save|pin|unpin|remove|delete)\\b|\\bmy (saved )?(cities|pins?|list|locations)\\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex InScopePattern();

    public string Name => "Code Input";

    public Task<ChatScopeGateResult> EvaluateAsync(string text, CancellationToken cancellationToken)
    {
        if (DenyListPattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message matches an off-topic/instruction-override pattern"));
        }

        if (!InScopePattern().IsMatch(text))
        {
            return Task.FromResult(new ChatScopeGateResult(
                false, "message does not contain a weather, location or saved-city keyword"));
        }

        return Task.FromResult(new ChatScopeGateResult(true, null));
    }
}
