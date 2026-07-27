using Microsoft.Extensions.Configuration;

namespace WeatherWorkerDotNet;

/// <summary>
/// Thresholds for Hangfire health checks surfaced on the worker About endpoint.
/// </summary>
public sealed class HangfireAboutHealthOptions
{
    public const string StaleProcessingMinutesKey = "HangfireAboutHealth_StaleProcessingMinutes";
    public const string StaleEnqueuedMinutesKey = "HangfireAboutHealth_StaleEnqueuedMinutes";

    /// <summary>
    /// Processing jobs running longer than this are treated as unhealthy.
    /// </summary>
    public int StaleProcessingMinutes { get; set; } = 30;

    /// <summary>
    /// Enqueued jobs waiting longer than this are treated as unhealthy.
    /// </summary>
    public int StaleEnqueuedMinutes { get; set; } = 60;

    public static HangfireAboutHealthOptions Bind(IConfiguration configuration)
    {
        var options = new HangfireAboutHealthOptions();
        options.StaleProcessingMinutes = configuration.GetValue(
            StaleProcessingMinutesKey,
            options.StaleProcessingMinutes);
        options.StaleEnqueuedMinutes = configuration.GetValue(
            StaleEnqueuedMinutesKey,
            options.StaleEnqueuedMinutes);
        return options;
    }
}
