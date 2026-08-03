using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Core.Hangfire;

public static class HangfireMediatRExtensions
{
    public static void AddOrUpdateMediatREvent<TEvent>(
        this IRecurringJobManager recurringJobs,
        string recurringJobId,
        string cronExpression,
        TEvent @event,
        string queue = "default")
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(recurringJobs);
        ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentNullException.ThrowIfNull(@event);

        var eventTypeName = HangfireMediatREventSerializer.GetTypeName(typeof(TEvent));
        var eventJson = HangfireMediatREventSerializer.Serialize(@event);

        recurringJobs.AddOrUpdate<HangfireMediatREventJob>(
            recurringJobId,
            queue,
            job => job.SendAsync(eventTypeName, eventJson, CancellationToken.None),
            cronExpression);
    }

    public static void AddOrUpdateMediatREvent<TEvent>(
        this IRecurringJobManager recurringJobs,
        string recurringJobId,
        string cronExpression,
        string queue = "default")
        where TEvent : class, new()
    {
        recurringJobs.AddOrUpdateMediatREvent(recurringJobId, cronExpression, new TEvent(), queue);
    }

    public static string EnqueueMediatREvent<TEvent>(
        this IBackgroundJobClient backgroundJobs,
        TEvent @event,
        string queue = "default")
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(backgroundJobs);
        ArgumentNullException.ThrowIfNull(@event);

        var eventTypeName = HangfireMediatREventSerializer.GetTypeName(typeof(TEvent));
        var eventJson = HangfireMediatREventSerializer.Serialize(@event);

        return backgroundJobs.Create(
            Job.FromExpression<HangfireMediatREventJob>(
                job => job.SendAsync(eventTypeName, eventJson, CancellationToken.None)),
            new EnqueuedState(queue));
    }
}
