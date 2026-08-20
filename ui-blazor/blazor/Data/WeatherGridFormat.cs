namespace WeatherBlazor.Data;

using System.Globalization;

/// <summary>Formatting helpers for the weather modal's forecast/history grid tabs.</summary>
public static class WeatherGridFormat
{
    /// <summary>Formats an Open-Meteo daily date ("2026-08-19") as "Wed, Aug 19".</summary>
    public static string FormatCalendarDate(string isoDate)
    {
        if (!DateTime.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return isoDate ?? string.Empty;
        }

        return date.ToString("ddd, MMM d", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats an Open-Meteo hourly/15-minute timestamp ("2026-08-19T14:00") as "2 PM"
    /// (minutes shown only when non-zero, e.g. "2:15 PM").
    /// </summary>
    public static string FormatClockTime(string isoDateTime)
    {
        if (!DateTime.TryParse(isoDateTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            return isoDateTime ?? string.Empty;
        }

        return dateTime.ToString(dateTime.Minute == 0 ? "h tt" : "h:mm tt", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats an already-converted inches value (the API returns US customary units) rounded to the nearest 1/16", e.g. "1 1/2"". Negative values (an upstream data artifact) are treated as zero.</summary>
    public static string FormatPrecipitationIn(double inches)
    {
        if (double.IsNaN(inches) || double.IsInfinity(inches))
        {
            return string.Empty;
        }

        var sixteenths = (long)Math.Round(Math.Max(0, inches) * 16, MidpointRounding.AwayFromZero);
        var whole = sixteenths / 16;
        var remainder = sixteenths % 16;

        if (remainder == 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{whole}\"");
        }

        var (numerator, denominator) = ReduceSixteenths(remainder);
        return whole == 0
            ? string.Create(CultureInfo.InvariantCulture, $"{numerator}/{denominator}\"")
            : string.Create(CultureInfo.InvariantCulture, $"{whole} {numerator}/{denominator}\"");
    }

    /// <summary>Reduces a sixteenths-of-an-inch numerator to lowest terms (denominator is always a power of two).</summary>
    private static (long Numerator, long Denominator) ReduceSixteenths(long numerator)
    {
        long denominator = 16;
        while (numerator != 0 && numerator % 2 == 0 && denominator > 1)
        {
            numerator /= 2;
            denominator /= 2;
        }
        return (numerator, denominator);
    }

    /// <summary>Formats an already-converted °F value (the API returns US customary units).</summary>
    public static string FormatTemperatureF(double fahrenheit) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(fahrenheit, 1)} °F");

    /// <summary>Formats an already-converted mph value (the API returns US customary units).</summary>
    public static string FormatWindSpeedMph(double mph) =>
        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(mph, 1)} mph");

    /// <summary>Wraps meteorological source degrees to 0–360. NaN / Infinity become 0.</summary>
    public static int NormalizeSourceDegrees(double degrees)
    {
        if (double.IsNaN(degrees) || double.IsInfinity(degrees))
        {
            return 0;
        }

        return (int)Math.Round(((degrees % 360d) + 360d) % 360d, MidpointRounding.AwayFromZero);
    }

    /// <summary>Formats API-provided compass plus source degrees, e.g. "SW (224°)".</summary>
    public static string FormatWindDirection(string compass, double degrees)
    {
        var label = (compass ?? string.Empty).Trim();
        var withDegrees = string.Create(CultureInfo.InvariantCulture, $"({NormalizeSourceDegrees(degrees)}°)");
        return label.Length == 0 ? withDegrees : $"{label} {withDegrees}";
    }

    /// <summary>Formats a run-log timestamp as UTC, e.g. "14:32:07.123", regardless of the input's DateTimeKind.</summary>
    public static string FormatRunLogTimestamp(DateTime utc) =>
        utc.ToUniversalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

    /// <summary>Formats a millisecond duration with thousands separators, e.g. "1,234".</summary>
    public static string FormatRunLogMs(int milliseconds) =>
        milliseconds.ToString("#,##0", CultureInfo.InvariantCulture);

    /// <summary>Formats a run-log token count with thousands separators, e.g. "1,234". Null becomes an empty string.</summary>
    public static string FormatRunLogTokenCount(int? tokens) =>
        tokens?.ToString("#,##0", CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>Formats a compact duration, e.g. "842ms" or "1.24s".</summary>
    public static string FormatChatRuntime(int milliseconds)
    {
        if (milliseconds < 1000)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{milliseconds}ms");
        }

        var seconds = milliseconds / 1000.0;
        return string.Create(CultureInfo.InvariantCulture, $"{seconds:0.##}s");
    }

    /// <summary>Formats the visible assistant-row chip, e.g. "1.24s · 4,218 tok".</summary>
    public static string FormatChatUsageChip(ChatUsage? usage)
    {
        if (usage is null)
        {
            return string.Empty;
        }

        var runtime = FormatChatRuntime(usage.RuntimeMs);
        var tokens = FormatRunLogTokenCount(usage.TotalTokenCount);
        return string.IsNullOrEmpty(tokens) ? runtime : $"{runtime} · {tokens} tok";
    }

    /// <summary>Formats the hover breakdown for a usage chip.</summary>
    public static string FormatChatUsageDetails(ChatUsage? usage)
    {
        if (usage is null)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            $"Runtime: {FormatRunLogMs(usage.RuntimeMs)} ms",
        };

        AddTokenLine(lines, "Input", usage.InputTokenCount);
        AddTokenLine(lines, "Cached", usage.CachedTokenCount);
        AddTokenLine(lines, "Output", usage.OutputTokenCount);
        AddTokenLine(lines, "Reasoning", usage.ReasoningTokenCount);
        AddTokenLine(lines, "Total", usage.TotalTokenCount);
        return string.Join('\n', lines);
    }

    private static void AddTokenLine(List<string> lines, string label, int? tokens)
    {
        var formatted = FormatRunLogTokenCount(tokens);
        if (formatted.Length > 0)
        {
            lines.Add($"{label}: {formatted}");
        }
    }
}
