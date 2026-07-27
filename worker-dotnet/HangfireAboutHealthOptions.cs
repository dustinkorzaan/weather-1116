namespace WeatherWorkerDotNet;

/// <summary>
/// Thresholds for Hangfire health checks surfaced on the worker About endpoint.
/// </summary>
public sealed class HangfireAboutHealthOptions
{
    public const string SectionName = "HangfireAboutHealth";

    /// <summary>
    /// Processing jobs running longer than this are treated as unhealthy.
    /// </summary>
    public int StaleProcessingMinutes { get; set; } = 30;

    /// <summary>
    /// Enqueued jobs waiting longer than this are treated as unhealthy.
    /// </summary>
    public int StaleEnqueuedMinutes { get; set; } = 60;
}
