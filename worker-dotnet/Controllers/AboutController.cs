using Core.about;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeatherWorkerDotNet.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public sealed class AboutController(
    ILogger<AboutController> logger) : ControllerBase
{
    [HttpGet]
    public ActionResult<AboutNode> Get()
    {
        var workerNode = AboutTreeBuilder.BuildWorkerDotNetNode();
        var hangfireNode = BuildHangfireNode();
        return Ok(AboutTreeBuilder.BuildWorkerRoot(workerNode, hangfireNode));
    }

    private AboutNode BuildHangfireNode()
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();
            var statistics = monitoringApi.GetStatistics();
            var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue)
                .Select(item => item.Value)
                .ToList();
            var enqueuedJobs = monitoringApi
                .Queues()
                .SelectMany(queue => monitoringApi.EnqueuedJobs(queue.Name, 0, int.MaxValue))
                .Select(item => item.Value)
                .ToList();

            var now = DateTime.UtcNow;
            var hasStaleProcessing = processingJobs.Any(job =>
                job.StartedAt.HasValue &&
                now - job.StartedAt.Value > TimeSpan.FromMinutes(30));
            var hasStaleEnqueued = enqueuedJobs.Any(job =>
                job.EnqueuedAt.HasValue &&
                now - job.EnqueuedAt.Value > TimeSpan.FromMinutes(60));

            return new AboutNode
            {
                Name = "Hangfire",
                PublicMessage = $"{statistics.Failed} failed, {statistics.Processing} processing, {statistics.Enqueued} enqueued",
                IsHealthy = statistics.Failed == 0 && !hasStaleProcessing && !hasStaleEnqueued,
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not read Hangfire monitoring statistics");
            return new AboutNode
            {
                Name = "Hangfire",
                PublicMessage = "Unable to read Hangfire statistics",
                IsHealthy = false,
            };
        }
    }
}
