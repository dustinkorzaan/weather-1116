using System.Globalization;
using Core.Geo.Models;

namespace Core.Geo;

internal static class NominatimLocationMapper
{
    public static string FromReverse(NominatimReverseResponse? geoData, double latitude, double longitude)
    {
        var structured = FromAddress(geoData?.Address);
        if (structured.Length > 0)
        {
            return structured;
        }

        var name = geoData?.Name.Trim() ?? string.Empty;
        if (name.Length > 0)
        {
            return name;
        }

        return FormatCoordinates(latitude, longitude);
    }

    public static string FromAddress(NominatimAddress? address)
    {
        if (address is null)
        {
            return string.Empty;
        }

        var city = FirstNonEmpty(
            address.City,
            address.Town,
            address.Village,
            address.Municipality,
            address.County);
        var state = address.State.Trim();
        var country = address.Country.Trim();
        var isUs = string.Equals(address.CountryCode, "us", StringComparison.OrdinalIgnoreCase);

        var parts = new List<string>(3);
        if (city.Length > 0)
        {
            parts.Add(city);
        }

        if (state.Length > 0)
        {
            parts.Add(state);
        }

        if (!isUs && country.Length > 0)
        {
            parts.Add(country);
        }

        return string.Join(", ", parts);
    }

    internal static string FormatCoordinates(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatHemisphereDegrees(latitude, "N", "S")}, {FormatHemisphereDegrees(longitude, "E", "W")}");

    private static string FormatHemisphereDegrees(double value, string positiveLabel, string negativeLabel)
    {
        var hemisphere = value >= 0 ? positiveLabel : negativeLabel;
        return string.Create(CultureInfo.InvariantCulture, $"{Math.Abs(value):0.00}\u00B0 {hemisphere}");
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            var trimmed = value.Trim();
            if (trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        return string.Empty;
    }
}
