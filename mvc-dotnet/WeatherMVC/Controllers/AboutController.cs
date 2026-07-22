using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

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
            AboutEndpointUrls.ToAboutUrl(configuration["MCP_DOTNET_URL"]),
            "mcp-dotnet",
            cancellationToken);
        var mcpFunctionTask = aboutClient.GetAsync(
            AboutEndpointUrls.ToAboutUrl(configuration["MCP_FUNCTION_URL"]),
            "mcp-function",
            cancellationToken);

        await Task.WhenAll(mcpDotNetTask, mcpFunctionTask);

        var root = AboutTreeBuilder.BuildMvcRoot(
            await mcpDotNetTask,
            await mcpFunctionTask);
        return Ok(root);
    }
}
