using Core.about;
using Core.demo.handlers;

namespace WeatherBlazor.Data;

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

        return AboutTreeBuilder.BuildRoot("Blazor Root", blazorNode, apiRoot);
    }
}
