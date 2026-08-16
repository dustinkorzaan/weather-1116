using Core.Geo.Models;

namespace Core.Geo;

internal static class NominatimLocationMapper
{
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
