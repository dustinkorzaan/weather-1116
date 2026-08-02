using Core.About;
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
        var mcpAppKey = configuration["MCP_APP_KEY"];
        var isHealthy = !string.IsNullOrWhiteSpace(mcpAppKey) && tools.Any(tool =>
            string.Equals(tool.ProtocolTool.Name, expectedTool, StringComparison.Ordinal));

        return Ok(AboutTreeBuilder.BuildMcpDotNetNode(isHealthy));
    }
}
