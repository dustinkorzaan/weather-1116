using Hangfire;

namespace WeatherWorkerDotNet;

/// <summary>
/// Keeps this replica from scaling to zero while a Hangfire job is still running. ACA's HTTP
/// scaler only counts inbound requests, so a long job (import-cities on the Basic SQL tier) that
/// outlives the 30-minute cooldown after the last request is killed mid-run: SIGTERM trips
/// Hangfire's shutdown token and the job fails with "Operation cancelled by user". While any job
/// is processing, this pings the replica's own /Wake through ACA ingress (CONTAINER_APP_HOSTNAME,
/// set by Container Apps), which resets that cooldown. Outside ACA the variable is unset and this
/// does nothing.
/// </summary>
public sealed class ProcessingJobKeepAlive(
    JobStorage jobStorage,
    IHttpClientFactory clientFactory,
    IConfiguration configuration,
    ILogger<ProcessingJobKeepAlive> logger) : BackgroundService
{
    // Well inside container-app.bicep's 1800s cooldownPeriod.
    internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var wakeUrl = BuildWakeUrl(configuration["CONTAINER_APP_HOSTNAME"]);
        if (wakeUrl is null)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (jobStorage.GetMonitoringApi().GetStatistics().Processing == 0)
                {
                    continue;
                }

                using var client = clientFactory.CreateClient();
                using var response = await client.GetAsync(wakeUrl, stoppingToken);
                logger.LogInformation("Hangfire job processing; pinged {WakeUrl} ({StatusCode}) to hold off scale-in", wakeUrl, (int)response.StatusCode);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Keep-alive ping to {WakeUrl} failed", wakeUrl);
            }
        }
    }

    internal static Uri? BuildWakeUrl(string? containerAppHostname) =>
        string.IsNullOrWhiteSpace(containerAppHostname)
            ? null
            : new Uri($"https://{containerAppHostname.Trim()}/Wake");
}
