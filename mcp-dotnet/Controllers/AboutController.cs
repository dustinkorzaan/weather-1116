using Core.about;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace WeatherMcpDotNet.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public sealed class AboutController(
    IEnumerable<McpServerTool> tools,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public ActionResult<AboutNode> Get()
    {
        const string expectedTool = "GetPublicWeatherData";
        var mcpApiKey = configuration["MCP_API_KEY"];
        var isHealthy = !string.IsNullOrWhiteSpace(mcpApiKey) && tools.Any(tool =>
            string.Equals(tool.ProtocolTool.Name, expectedTool, StringComparison.Ordinal));

        return Ok(AboutTreeBuilder.BuildMcpDotNetNode(isHealthy));
    }
}
