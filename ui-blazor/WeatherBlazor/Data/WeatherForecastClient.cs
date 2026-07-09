namespace WeatherBlazor.Data;

public class HelloWorldResponse
{
    public required string RequestMessage { get; set; }
    public required string RequestResponse { get; set; }
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
        => await _httpClient.GetFromJsonAsync<WeatherForecast[]>($"WeatherForecast?startDate={startDate}") ?? [];

    public async Task<HelloWorldResponse?> GetHelloAsync()
        => await _httpClient.GetFromJsonAsync<HelloWorldResponse>("Home/Hello");

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
}
