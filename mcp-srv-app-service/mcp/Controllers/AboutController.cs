using Core.About;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Controllers;

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
        var mcpSrvAppServiceKey = configuration["MCP_SRV_APP_SERVICE_KEY"];
        var isHealthy = !string.IsNullOrWhiteSpace(mcpSrvAppServiceKey) && tools.Any(tool =>
            string.Equals(tool.ProtocolTool.Name, expectedTool, StringComparison.Ordinal));

        return Ok(AboutTreeBuilder.BuildMcpSrvAppServiceNode(isHealthy));
    }
}
