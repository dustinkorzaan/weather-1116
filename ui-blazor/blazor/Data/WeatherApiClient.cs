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

public class LatLongResponse
{
    public int Rank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class LocationResponse
{
    public string Location { get; set; } = string.Empty;
}

public class WeatherApiClient
{
    private HttpClient _httpClient;
    private ILogger<WeatherApiClient> _logger;

    public WeatherApiClient(HttpClient httpClient, ILogger<WeatherApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HelloWorldResponse?> GetHello()
        => await _httpClient.GetFromJsonAsync<HelloWorldResponse>("Home/Hello");

    public async Task<AIWeatherResponse?> GetCurrentAIWeather(string location)
    {
        var route = $"AIWeather/Current?location={Uri.EscapeDataString(location)}";
        return await _httpClient.GetFromJsonAsync<AIWeatherResponse>(route);
    }

    public async Task<LatLongResponse?> SearchLocation(string location)
    {
        var route = $"Geo?location={Uri.EscapeDataString(location)}";
        return await _httpClient.GetFromJsonAsync<LatLongResponse>(route);
    }

    public async Task<LocationResponse?> GetLocation(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var route = string.Create(
            CultureInfo.InvariantCulture,
            $"Geo/GetLocation?latitude={latitude}&longitude={longitude}");
        using var response = await _httpClient.GetAsync(route, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LocationResponse>(cancellationToken: cancellationToken);
    }

    public async Task<AboutNode> GetAbout()
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
            BuildBranchName = ResolveBuildBranchName(),
        };

        var children = new List<AboutNode> { blazorNode, apiRoot };

        return new AboutNode
        {
            Name = "Blazor Root",
            Children = children,
            IsHealthy = ComputeAggregateHealth(children),
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

    private static string? ResolveBuildBranchName()
    {
        var value = Environment.GetEnvironmentVariable("BUILD_BRANCH_NAME");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
