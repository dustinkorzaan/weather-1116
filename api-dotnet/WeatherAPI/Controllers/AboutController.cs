using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AboutController(
    IAboutClient aboutClient,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AboutNode>> Get(CancellationToken cancellationToken)
    {
        var mcpDotNetTask = aboutClient.GetAsync(
            $"{configuration["MCP_DOTNET_URL"]}/About",
            "mcp-dotnet",
            cancellationToken);
        var mcpFunctionTask = aboutClient.GetAsync(
            $"{configuration["MCP_FUNCTION_URL"]}/About",
            "mcp-function",
            cancellationToken);

        await Task.WhenAll(mcpDotNetTask, mcpFunctionTask);

        var root = AboutTreeBuilder.BuildApiRoot(
            await mcpDotNetTask,
            await mcpFunctionTask);
        return Ok(root);
    }
}
