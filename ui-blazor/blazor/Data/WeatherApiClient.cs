namespace WeatherBlazor.Data;

using System.Globalization;
using System.Text.Json.Serialization;

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
    public string WindDirectionSource { get; set; } = string.Empty;
    public int WindDirectionSourceDegrees { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
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

public enum PublicWeatherForecastResolution
{
    Daily,
    Hourly,
    FifteenMinutes,
}

public enum PublicWeatherHistoryResolution
{
    Daily,
    Hourly,
}

public class UIWeatherForecastResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("hourly")]
    public UIWeatherHourlySeries? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public UIWeatherDailySeries? Daily { get; set; }

    [JsonPropertyName("minutely15")]
    public UIWeatherHourlySeries? Minutely15 { get; set; }
}

public class UIWeatherHistoryResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("hourly")]
    public UIWeatherHourlySeries? Hourly { get; set; }

    [JsonPropertyName("daily")]
    public UIWeatherDailySeries? Daily { get; set; }
}

/// <summary>Shared shape for the forecast/history "hourly" and "minutely15" series, in US customary units.</summary>
public class UIWeatherHourlySeries
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperatureF")]
    public List<double> TemperatureF { get; set; } = [];

    [JsonPropertyName("precipitationInch")]
    public List<double> PrecipitationInch { get; set; } = [];

    [JsonPropertyName("weatherCode")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("windSpeedMPH")]
    public List<double> WindSpeedMPH { get; set; } = [];

    [JsonPropertyName("windDirectionSourceDegrees")]
    public List<int> WindDirectionSourceDegrees { get; set; } = [];

    [JsonPropertyName("windDirectionSource")]
    public List<string> WindDirectionSource { get; set; } = [];
}

/// <summary>Shared shape for the forecast/history "daily" series, in US customary units.</summary>
public class UIWeatherDailySeries
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("weatherCode")]
    public List<int> WeatherCode { get; set; } = [];

    [JsonPropertyName("temperatureHighF")]
    public List<double> TemperatureHighF { get; set; } = [];

    [JsonPropertyName("temperatureLowF")]
    public List<double> TemperatureLowF { get; set; } = [];

    [JsonPropertyName("precipitationInch")]
    public List<double> PrecipitationInch { get; set; } = [];

    [JsonPropertyName("windSpeedMPH")]
    public List<double> WindSpeedMPH { get; set; } = [];

    [JsonPropertyName("windDirectionSourceDegrees")]
    public List<int> WindDirectionSourceDegrees { get; set; } = [];

    [JsonPropertyName("windDirectionSource")]
    public List<string> WindDirectionSource { get; set; } = [];
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

    public async Task<UIWeatherForecastResponse?> GetForecast(
        double latitude,
        double longitude,
        PublicWeatherForecastResolution resolution = PublicWeatherForecastResolution.Daily,
        CancellationToken cancellationToken = default)
    {
        var route = string.Create(
            CultureInfo.InvariantCulture,
            $"Forecast?latitude={latitude}&longitude={longitude}&resolution={resolution}");
        return await _httpClient.GetFromJsonAsync<UIWeatherForecastResponse>(route, cancellationToken);
    }

    public async Task<UIWeatherHistoryResponse?> GetHistory(
        double latitude,
        double longitude,
        PublicWeatherHistoryResolution resolution = PublicWeatherHistoryResolution.Daily,
        CancellationToken cancellationToken = default)
    {
        var route = string.Create(
            CultureInfo.InvariantCulture,
            $"History?latitude={latitude}&longitude={longitude}&resolution={resolution}");
        return await _httpClient.GetFromJsonAsync<UIWeatherHistoryResponse>(route, cancellationToken);
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
