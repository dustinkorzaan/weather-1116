using Core.Http.Events;
using MediatR;

namespace Core.Http.Handlers;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on failure.
/// Backoff starts at 200ms and doubles after each retry.
/// </summary>
public class GetThirdPartyStringWithRetryHandler : IRequestHandler<GetThirdPartyStringWithRetryEvent, string>
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private readonly HttpClient _httpClient;

    public GetThirdPartyStringWithRetryHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> Handle(
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestUri);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await SendGetAsync(_httpClient, request, cancellationToken);
            }
            catch when (attempt < RetryCount)
            {
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static async Task<string> SendGetAsync(
        HttpClient client,
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        if (request.Headers is not { Count: > 0 })
        {
            return await client.GetStringAsync(request.RequestUri, cancellationToken);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
