using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Configuration;
using WeatherWorkerDotNet;

namespace WeatherWorkerDotNet.Tests;

public class HangfireAboutHealthTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);
    private static readonly HangfireAboutHealthOptions DefaultOptions =
        HangfireAboutHealthOptions.Bind(new ConfigurationBuilder().Build());

    [Fact]
    public void IsHealthy_ReturnsFalse_WhenFailedJobsExist()
    {
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Failed = 1 },
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsTrue_WhenNoJobsAreActive()
    {
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto(),
        };

        Assert.True(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsFalse_WhenProcessingJobExceedsThreshold()
    {
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Processing = 1 },
            ProcessingJobsFactory = (_, _) => ToJobList<ProcessingJobDto>(
                [ProcessingJob(UtcNow - TimeSpan.FromMinutes(31))]),
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsTrue_WhenProcessingJobIsBelowThreshold()
    {
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Processing = 1 },
            ProcessingJobsFactory = (_, _) => ToJobList<ProcessingJobDto>(
                [ProcessingJob(UtcNow - TimeSpan.FromMinutes(29))]),
        };

        Assert.True(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsFalse_WhenStaleProcessingJobAppearsOnlyAfterSample()
    {
        var processingJobs = Enumerable.Range(0, 12)
            .Select(index => ProcessingJob(UtcNow - TimeSpan.FromMinutes(5)))
            .ToList();
        processingJobs[11] = ProcessingJob(UtcNow - TimeSpan.FromMinutes(45));

        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Processing = 12 },
            ProcessingJobsFactory = (from, count) => ToJobList(
                processingJobs.Skip(from).Take(count)),
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsFalse_WhenEnqueuedJobExceedsThreshold()
    {
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Enqueued = 1 },
            QueueList =
            [
                new QueueWithTopEnqueuedJobsDto
                {
                    Name = "default",
                    Length = 1,
                },
            ],
            EnqueuedJobsFactory = (_, _, _) => ToJobList<EnqueuedJobDto>(
                [EnqueuedJob(UtcNow - TimeSpan.FromMinutes(61))]),
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_ReturnsFalse_WhenStaleEnqueuedJobAppearsOnlyAfterSample()
    {
        var enqueuedJobs = Enumerable.Range(0, 12)
            .Select(_ => EnqueuedJob(UtcNow - TimeSpan.FromMinutes(5)))
            .ToList();
        enqueuedJobs[11] = EnqueuedJob(UtcNow - TimeSpan.FromMinutes(90));

        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Enqueued = 12 },
            QueueList =
            [
                new QueueWithTopEnqueuedJobsDto
                {
                    Name = "default",
                    Length = 12,
                },
            ],
            EnqueuedJobsFactory = (_, from, count) => ToJobList(
                enqueuedJobs.Skip(from).Take(count)),
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
    }

    [Fact]
    public void IsHealthy_UsesConfiguredThresholds()
    {
        var options = new HangfireAboutHealthOptions
        {
            StaleProcessingMinutes = 10,
            StaleEnqueuedMinutes = 20,
        };
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto { Processing = 1, Enqueued = 1 },
            ProcessingJobsFactory = (_, _) => ToJobList<ProcessingJobDto>(
                [ProcessingJob(UtcNow - TimeSpan.FromMinutes(11))]),
            QueueList =
            [
                new QueueWithTopEnqueuedJobsDto
                {
                    Name = "default",
                    Length = 1,
                },
            ],
            EnqueuedJobsFactory = (_, _, _) => ToJobList<EnqueuedJobDto>(
                [EnqueuedJob(UtcNow - TimeSpan.FromMinutes(15))]),
        };

        Assert.False(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, options));
    }

    [Fact]
    public void IsHealthy_DoesNotQueryProcessingJobs_WhenProcessingCountIsZero()
    {
        var queried = false;
        var api = new FakeMonitoringApi
        {
            Statistics = new StatisticsDto(),
            ProcessingJobsFactory = (_, _) =>
            {
                queried = true;
                return new JobList<ProcessingJobDto>([]);
            },
        };

        Assert.True(HangfireAboutHealth.IsHealthy(api, api.Statistics, UtcNow, DefaultOptions));
        Assert.False(queried);
    }

    private static ProcessingJobDto ProcessingJob(DateTime startedAt)
        => new() { StartedAt = startedAt };

    private static EnqueuedJobDto EnqueuedJob(DateTime enqueuedAt)
        => new() { EnqueuedAt = enqueuedAt };

    private static JobList<T> ToJobList<T>(IEnumerable<T> jobs)
        => new(jobs.Select((job, index) => KeyValuePair.Create(index.ToString(), job)));
}
