namespace Core.AIWeather;

/// <summary>Shared AI weather prompt fragments for wind direction source fields.</summary>
public static class AIWeatherSystemInstructions
{
    /// <summary>JSON field bullets for meteorological source degrees and derived compass label.</summary>
    public const string WindDirectionJsonFields = """
        - windDirectionSourceDegrees: Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
        - windDirectionSource: 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
        """;

    /// <summary>Guidance for wind direction in prose (fullSummary or chat replies).</summary>
    public const string WindDirectionSummaryGuidance =
        "When stating wind direction, use the meteorological source compass label from windDirectionSource (where the wind comes from), optionally with source degrees in parentheses (e.g. SW (224°)). Do not add 180 to degrees.";
}
