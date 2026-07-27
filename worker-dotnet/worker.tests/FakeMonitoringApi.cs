using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace WeatherWorkerDotNet.Tests;

internal sealed class FakeMonitoringApi : IMonitoringApi
{
    public StatisticsDto Statistics { get; init; } = new();
    public IReadOnlyList<QueueWithTopEnqueuedJobsDto> QueueList { get; init; } = [];
    public Func<int, int, JobList<ProcessingJobDto>>? ProcessingJobsFactory { get; init; }
    public Func<string, int, int, JobList<EnqueuedJobDto>>? EnqueuedJobsFactory { get; init; }

    public IList<QueueWithTopEnqueuedJobsDto> Queues() => QueueList.ToList();

    public JobList<ProcessingJobDto> ProcessingJobs(int from, int count)
        => ProcessingJobsFactory?.Invoke(from, count)
            ?? new JobList<ProcessingJobDto>([]);

    public JobList<EnqueuedJobDto> EnqueuedJobs(string queue, int from, int perPage)
        => EnqueuedJobsFactory?.Invoke(queue, from, perPage)
            ?? new JobList<EnqueuedJobDto>([]);

    public StatisticsDto GetStatistics() => Statistics;

    public IList<ServerDto> Servers() => throw new NotSupportedException();
    public JobDetailsDto JobDetails(string jobId) => throw new NotSupportedException();
    public JobList<FetchedJobDto> FetchedJobs(string queue, int from, int perPage) => throw new NotSupportedException();
    public JobList<ScheduledJobDto> ScheduledJobs(int from, int count) => throw new NotSupportedException();
    public JobList<SucceededJobDto> SucceededJobs(int from, int count) => throw new NotSupportedException();
    public JobList<FailedJobDto> FailedJobs(int from, int count) => throw new NotSupportedException();
    public JobList<DeletedJobDto> DeletedJobs(int from, int count) => throw new NotSupportedException();
    public long ScheduledCount() => throw new NotSupportedException();
    public long EnqueuedCount(string queue) => throw new NotSupportedException();
    public long FetchedCount(string queue) => throw new NotSupportedException();
    public long FailedCount() => throw new NotSupportedException();
    public long ProcessingCount() => throw new NotSupportedException();
    public long SucceededListCount() => throw new NotSupportedException();
    public long DeletedListCount() => throw new NotSupportedException();
    public IDictionary<DateTime, long> SucceededByDatesCount() => throw new NotSupportedException();
    public IDictionary<DateTime, long> FailedByDatesCount() => throw new NotSupportedException();
    public IDictionary<DateTime, long> HourlySucceededJobs() => throw new NotSupportedException();
    public IDictionary<DateTime, long> HourlyFailedJobs() => throw new NotSupportedException();
}
