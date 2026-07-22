using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Core.http;

/// <summary>
/// Shared helper for the Non-AI Open-Meteo HTTP calls used by the geo and
/// weather handlers. Reuses a single <see cref="HttpClient"/>, logs the
/// outgoing request, and deserializes the JSON response into
/// <typeparamref name="T"/>.
/// </summary>
public static class OpenMeteoJsonClient
{
    private static readonly HttpClient SharedClient = new();

    public static async Task<T?> GetAsync<T>(
        string url,
        ILogger logger,
        CancellationToken cancellationToken,
        JsonSerializerOptions? options = null)
    {
        logger.LogInformation("Non-AI: Fetching data from: {Url}", url);
        var json = await SharedClient.GetStringAsync(url, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, options);
    }
}
