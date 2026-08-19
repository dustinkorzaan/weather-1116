namespace WeatherMVC.Models;

/// <summary>
/// One version panel (V3/V4/V5) on the Current AI Weather page, see Views/Home/_CurrentAIWeatherPanel.cshtml.
/// </summary>
public class CurrentAIWeatherPanelViewModel
{
    /// <summary>Panel key used by currentAIWeatherTabs.js, e.g. "v3".</summary>
    public required string PanelKey { get; init; }

    /// <summary>Element id suffix, empty for V3 and "-v4"/"-v5" for the later versions.</summary>
    public required string IdSuffix { get; init; }

    /// <summary>MVC action rendering this panel's weather, e.g. "GetCurrentAIWeatherV3".</summary>
    public required string Action { get; init; }

    public bool Hidden { get; init; }

    public string ElementId(string name) => $"ai-weather-{name}{IdSuffix}";
}
