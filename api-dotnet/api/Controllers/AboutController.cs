using Core.About;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AboutController(
    IAboutClient aboutClient,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AboutNode>> Get(CancellationToken cancellationToken)
    {
        var workerDotNetTask = aboutClient.GetAsync(
            $"{configuration["WORKER_DOTNET_URL"]}/About",
            "Worker Root",
            cancellationToken);
        var mcpDotNetTask = aboutClient.GetAsync(
            $"{configuration["MCP_SRV_APP_SERVICE_URL"]}/About",
            "mcp-srv-app-service",
            cancellationToken);
        var mcpFunctionTask = aboutClient.GetAsync(
            $"{configuration["MCP_SRV_FUNC_APP_URL"]}/About",
            "mcp-srv-func-app",
            cancellationToken);

        await Task.WhenAll(workerDotNetTask, mcpDotNetTask, mcpFunctionTask);

        var root = AboutTreeBuilder.BuildApiRoot(
            await workerDotNetTask,
            await mcpDotNetTask,
            await mcpFunctionTask);
        return Ok(root);
    }
}
