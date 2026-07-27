using Microsoft.Extensions.Configuration;

namespace WeatherWorkerDotNet;

/// <summary>
/// Thresholds for Hangfire health checks surfaced on the worker About endpoint.
/// </summary>
public sealed class HangfireAboutHealthOptions
{
    public int StaleProcessingMinutes { get; set; }
    public int StaleEnqueuedMinutes { get; set; }

    public static HangfireAboutHealthOptions Bind(IConfiguration configuration)
        => new()
        {
            StaleProcessingMinutes = configuration.GetValue<int?>(
                "HangfireAboutHealth_StaleProcessingMinutes") ?? 30,
            StaleEnqueuedMinutes = configuration.GetValue<int?>(
                "HangfireAboutHealth_StaleEnqueuedMinutes") ?? 60,
        };
}
