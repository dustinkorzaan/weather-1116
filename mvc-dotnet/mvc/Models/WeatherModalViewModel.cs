using System.Globalization;

namespace WeatherMVC.Models;

/// <summary>
/// Backing model for the near-full-screen weather view opened from a map pin ("Phase 1"),
/// see Views/Home/Weather.cshtml.
/// </summary>
public class WeatherModalViewModel
{
    public static readonly IReadOnlyList<(string Value, string Label)> Tabs =
    [
        ("current", "Current AI Weather"),
        ("daily-forecast", "Daily Forecast"),
        ("hourly-forecast", "Hourly Forecast"),
        ("every-15-forecast", "Every 15 Forecast"),
        ("daily-history", "Daily History"),
        ("hourly-history", "Hourly History"),
    ];

    public const string DefaultTab = "current";

    public required string Name { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public required string Tab { get; init; }

    public required string Title { get; init; }

    /// <summary>The location string passed to GetCurrentAIWeatherV3, e.g. "Nashville, TN (36.1627&#176; N, 86.7816&#176; W)".</summary>
    public required string LocationQuery { get; init; }

    public static WeatherModalViewModel Create(string? name, double? lat, double? lng, string? tab)
    {
        var trimmedName = (name ?? string.Empty).Trim();
        var resolvedTab = Tabs.Any(t => t.Value == tab) ? tab! : DefaultTab;
        var withCoordinates = FormatWithLatLong(trimmedName, lat, lng);
        var title = string.IsNullOrWhiteSpace(withCoordinates)
            ? (string.IsNullOrWhiteSpace(trimmedName) ? "Location" : trimmedName)
            : withCoordinates;

        return new WeatherModalViewModel
        {
            Name = trimmedName,
            Latitude = lat,
            Longitude = lng,
            Tab = resolvedTab,
            Title = title,
            LocationQuery = string.IsNullOrWhiteSpace(withCoordinates) ? trimmedName : withCoordinates,
        };
    }

    private static string FormatWithLatLong(string name, double? lat, double? lng)
    {
        if (string.IsNullOrWhiteSpace(name) || lat is null || lng is null)
        {
            return string.Empty;
        }

        return $"{name} ({FormatHemisphereDegrees(lat.Value, "N", "S")}, {FormatHemisphereDegrees(lng.Value, "E", "W")})";
    }

    private static string FormatHemisphereDegrees(double value, string positiveLabel, string negativeLabel)
    {
        var hemisphere = value >= 0 ? positiveLabel : negativeLabel;
        return $"{Math.Abs(value).ToString("0.0000", CultureInfo.InvariantCulture)}° {hemisphere}";
    }
}
