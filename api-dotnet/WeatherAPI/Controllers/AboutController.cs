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
            AboutEndpointUrls.ToAboutUrl(configuration["DotNetUrl"]),
            "mcp-dotnet",
            cancellationToken);
        var mcpFunctionTask = aboutClient.GetAsync(
            AboutEndpointUrls.ToAboutUrl(configuration["FunctionUrl"]),
            "mcp-function",
            cancellationToken);

        await Task.WhenAll(mcpDotNetTask, mcpFunctionTask);

        var root = AboutTreeBuilder.BuildApiRoot(
            await mcpDotNetTask,
            await mcpFunctionTask);
        return Ok(root);
    }
}
