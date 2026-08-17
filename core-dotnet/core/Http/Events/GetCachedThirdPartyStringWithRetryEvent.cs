using MediatR;

namespace Core.Http.Events;

/// <summary>
/// GET a URL (Open-Meteo, Nominatim, etc.) with retries on transient HTTPS failures.
/// </summary>
public class GetCachedThirdPartyStringWithRetryEvent : IRequest<string>
{
    public required string RequestUri { get; set; }

    public Dictionary<string, string> Headers { get; set; } = [];

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);
}
