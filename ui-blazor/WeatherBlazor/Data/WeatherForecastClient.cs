namespace WeatherBlazor.Data;

using System.Globalization;

public class HelloWorldResponse
{
    public required string RequestMessage { get; set; }
    public required string RequestResponse { get; set; }
}

public class AIWeatherResponse
{
    public string FullSummary { get; set; } = string.Empty;
    public double TemperatureF { get; set; }
    public double WindSpeedMPH { get; set; }
    public string WindDirection { get; set; } = string.Empty;
    public string Conditions { get; set; } = string.Empty;
}

public class AIWeatherStreamUpdate
{
    public string Type { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Delta { get; set; }
    public AIWeatherResponse? Result { get; set; }
}

public class WeatherForecastClient
{
    private HttpClient _httpClient;
    private ILogger<WeatherForecastClient> _logger;

    public WeatherForecastClient(HttpClient httpClient, ILogger<WeatherForecastClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherForecast[]> GetForecastAsync(DateTime? startDate)
    {
        var route = startDate.HasValue
            ? $"WeatherForecast?startDate={Uri.EscapeDataString(startDate.Value.ToString("O"))}"
            : "WeatherForecast";

        return await _httpClient.GetFromJsonAsync<WeatherForecast[]>(route) ?? [];
    }

    public async Task<HelloWorldResponse?> GetHelloAsync()
        => await _httpClient.GetFromJsonAsync<HelloWorldResponse>("Home/Hello");

    public async Task<AIWeatherResponse?> GetCurrentAIWeatherAsync(string location)
    {
        var route = $"AIWeather/Current?location={Uri.EscapeDataString(location)}";
        return await _httpClient.GetFromJsonAsync<AIWeatherResponse>(route);
    }

    public async Task StreamCurrentAIWeatherAsync(
        string location,
        Action<AIWeatherStreamUpdate> onUpdate,
        CancellationToken cancellationToken = default)
    {
        var route = $"AIWeather/Current/stream?location={Uri.EscapeDataString(location)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var update = System.Text.Json.JsonSerializer.Deserialize<AIWeatherStreamUpdate>(
                line["data: ".Length..],
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (update is null)
            {
                continue;
            }

            onUpdate(update);

            if (update.Type == "error")
            {
                throw new InvalidOperationException(update.Message ?? "Unable to load AI weather.");
            }
        }
    }

    public async Task<AboutNode> GetAboutAsync()
    {
        AboutNode apiRoot;

        try
        {
            apiRoot = await _httpClient.GetFromJsonAsync<AboutNode>("About")
                ?? new AboutNode { Name = "API Root", IsHealthy = false };
        }
        catch
        {
            apiRoot = new AboutNode { Name = "API Root", IsHealthy = false };
        }

        var blazorNode = new AboutNode
        {
            Name = "Blazor",
            IsHealthy = true,
            BuildNumber = ResolveBuildNumber(),
            BuildStart = ResolveBuildStart(),
        };

        var children = new List<AboutNode> { blazorNode, apiRoot };

        return new AboutNode
        {
            Name = "Blazor Root",
            Children = children,
            IsHealthy = ComputeAggregateHealth(children),
            BuildNumber = ResolveBuildNumber(),
            BuildStart = ResolveBuildStart(),
        };
    }

    private static bool ComputeAggregateHealth(IEnumerable<AboutNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.IsHealthy || !ComputeAggregateHealth(node.Children))
            {
                return false;
            }
        }

        return true;
    }

    private static int? ResolveBuildNumber()
    {
        var value = Environment.GetEnvironmentVariable("BUILD_NUMBER");
        return int.TryParse(value, out var number) ? number : null;
    }

    private static DateTime? ResolveBuildStart()
    {
        var value = Environment.GetEnvironmentVariable("BUILD_START");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var startedAt))
        {
            return startedAt;
        }

        return null;
    }
}
