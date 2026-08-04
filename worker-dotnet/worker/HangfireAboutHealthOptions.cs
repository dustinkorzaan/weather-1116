using Microsoft.Extensions.Configuration;

namespace WeatherWorkerDotNet;

/// <summary>
/// Thresholds for Hangfire health checks surfaced on the worker About endpoint.
/// </summary>
public sealed class HangfireAboutHealthOptions
{
    public int StaleProcessingMinutes { get; set; } = 30;
    public int StaleEnqueuedMinutes { get; set; } = 60;

    public static HangfireAboutHealthOptions Bind(IConfiguration configuration)
    {
        var options = new HangfireAboutHealthOptions();
        Configure(options, configuration);
        return options;
    }

    public static void Configure(HangfireAboutHealthOptions options, IConfiguration configuration)
    {
        options.StaleProcessingMinutes = configuration.GetValue<int?>(
            "HangfireAboutHealth_StaleProcessingMinutes") ?? 30;
        options.StaleEnqueuedMinutes = configuration.GetValue<int?>(
            "HangfireAboutHealth_StaleEnqueuedMinutes") ?? 60;
    }
}
