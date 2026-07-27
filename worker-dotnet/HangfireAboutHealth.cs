using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace WeatherWorkerDotNet;

/// <summary>
/// Evaluates Hangfire health for the worker About probe using cheap statistics first,
/// then sampling job details only when staleness checks are required.
/// </summary>
internal static class HangfireAboutHealth
{
    private const int SampleSize = 10;
    private static readonly TimeSpan StaleProcessingThreshold = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan StaleEnqueuedThreshold = TimeSpan.FromMinutes(60);

    public static bool IsHealthy(IMonitoringApi monitoringApi, StatisticsDto statistics, DateTime utcNow)
    {
        if (statistics.Failed > 0)
        {
            return false;
        }

        if (HasStaleProcessingJobs(monitoringApi, statistics, utcNow))
        {
            return false;
        }

        if (HasStaleEnqueuedJobs(monitoringApi, statistics, utcNow))
        {
            return false;
        }

        return true;
    }

    private static bool HasStaleProcessingJobs(
        IMonitoringApi monitoringApi,
        StatisticsDto statistics,
        DateTime utcNow)
    {
        if (statistics.Processing == 0)
        {
            return false;
        }

        var processingCount = ToIntCount(statistics.Processing);
        var sample = GetProcessingJobs(monitoringApi, 0, Math.Min(SampleSize, processingCount));
        if (ContainsStaleProcessing(sample, utcNow))
        {
            return true;
        }

        if (processingCount <= SampleSize)
        {
            return false;
        }

        var all = GetProcessingJobs(monitoringApi, 0, processingCount);
        return ContainsStaleProcessing(all, utcNow);
    }

    private static bool HasStaleEnqueuedJobs(
        IMonitoringApi monitoringApi,
        StatisticsDto statistics,
        DateTime utcNow)
    {
        if (statistics.Enqueued == 0)
        {
            return false;
        }

        foreach (var queue in monitoringApi.Queues())
        {
            if (queue.Length == 0)
            {
                continue;
            }

            var queueLength = ToIntCount(queue.Length);
            var sampleCount = Math.Min(SampleSize, queueLength);
            var sample = GetEnqueuedJobs(monitoringApi, queue.Name, 0, sampleCount);
            if (ContainsStaleEnqueued(sample, utcNow))
            {
                return true;
            }

            if (queueLength <= SampleSize)
            {
                continue;
            }

            var all = GetEnqueuedJobs(monitoringApi, queue.Name, 0, queueLength);
            if (ContainsStaleEnqueued(all, utcNow))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsStaleProcessing(IEnumerable<ProcessingJobDto> jobs, DateTime utcNow)
        => jobs.Any(job =>
            job.StartedAt.HasValue &&
            utcNow - job.StartedAt.Value > StaleProcessingThreshold);

    private static bool ContainsStaleEnqueued(IEnumerable<EnqueuedJobDto> jobs, DateTime utcNow)
        => jobs.Any(job =>
            job.EnqueuedAt.HasValue &&
            utcNow - job.EnqueuedAt.Value > StaleEnqueuedThreshold);

    private static List<ProcessingJobDto> GetProcessingJobs(
        IMonitoringApi monitoringApi,
        int from,
        int count)
        => monitoringApi.ProcessingJobs(from, count)
            .Select(item => item.Value)
            .ToList();

    private static List<EnqueuedJobDto> GetEnqueuedJobs(
        IMonitoringApi monitoringApi,
        string queueName,
        int from,
        int count)
        => monitoringApi.EnqueuedJobs(queueName, from, count)
            .Select(item => item.Value)
            .ToList();

    private static int ToIntCount(long count)
        => count > int.MaxValue ? int.MaxValue : (int)count;
}
