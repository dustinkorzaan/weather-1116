using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Core.Hangfire;

public static class HangfireCQMediatorExtensions
{
    public static void AddOrUpdateCQMediatorEvent<TEvent>(
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

        var eventDisplayName = HangfireCQMediatorEventSerializer.GetDisplayName(typeof(TEvent));
        var eventTypeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(TEvent));
        var eventJson = HangfireCQMediatorEventSerializer.Serialize(@event);

        recurringJobs.AddOrUpdate<HangfireCQMediatorEventJob>(
            recurringJobId,
            queue,
            job => job.SendAsync(eventDisplayName, eventTypeName, eventJson, CancellationToken.None),
            cronExpression);
    }

    public static void AddOrUpdateCQMediatorEvent<TEvent>(
        this IRecurringJobManager recurringJobs,
        string recurringJobId,
        string cronExpression,
        string queue = "default")
        where TEvent : class, new()
    {
        recurringJobs.AddOrUpdateCQMediatorEvent(recurringJobId, cronExpression, new TEvent(), queue);
    }

    public static string EnqueueCQMediatorEvent<TEvent>(
        this IBackgroundJobClient backgroundJobs,
        TEvent @event,
        string queue = "default")
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(backgroundJobs);
        ArgumentNullException.ThrowIfNull(@event);

        var eventDisplayName = HangfireCQMediatorEventSerializer.GetDisplayName(typeof(TEvent));
        var eventTypeName = HangfireCQMediatorEventSerializer.GetTypeName(typeof(TEvent));
        var eventJson = HangfireCQMediatorEventSerializer.Serialize(@event);

        return backgroundJobs.Create(
            Job.FromExpression<HangfireCQMediatorEventJob>(
                job => job.SendAsync(eventDisplayName, eventTypeName, eventJson, CancellationToken.None)),
            new EnqueuedState(queue));
    }
}
