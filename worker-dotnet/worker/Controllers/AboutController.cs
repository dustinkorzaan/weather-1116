using Core.About;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace WeatherWorkerDotNet.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public sealed class AboutController(
    ILogger<AboutController> logger,
    IOptions<HangfireAboutHealthOptions> hangfireHealthOptions) : ControllerBase
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
            return new AboutNode
            {
                Name = "Hangfire",
                PublicMessage = $"{statistics.Failed} failed, {statistics.Processing} processing, {statistics.Enqueued} enqueued",
                IsHealthy = HangfireAboutHealth.IsHealthy(
                    monitoringApi,
                    statistics,
                    DateTime.UtcNow,
                    hangfireHealthOptions.Value),
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
