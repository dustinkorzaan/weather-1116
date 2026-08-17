using MediatR;

namespace Core.Http.Events;

/// <summary>
/// GET a URL (Open-Meteo, Nominatim, etc.) with retries on transient HTTPS failures.
/// </summary>
public class GetThirdPartyStringWithRetryEvent : IRequest<string>
{
    public required string RequestUri { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();
}
