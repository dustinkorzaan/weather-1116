using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

[ApiController]
[Route("[controller]")]
public class AboutController(
    IMcpAboutClient mcpAboutClient,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AboutNode>> Get(CancellationToken cancellationToken)
    {
        var mcpDotNetTask = mcpAboutClient.GetAsync(
            configuration["McpAbout:DotNetUrl"],
            "mcp-dotnet",
            cancellationToken);
        var mcpFunctionTask = mcpAboutClient.GetAsync(
            configuration["McpAbout:FunctionUrl"],
            "mcp-function",
            cancellationToken);

        await Task.WhenAll(mcpDotNetTask, mcpFunctionTask);

        var root = AboutTreeBuilder.BuildMvcRoot(
            await mcpDotNetTask,
            await mcpFunctionTask);
        return Ok(root);
    }
}
