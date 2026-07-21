using Core.about;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace WeatherMcpFunction;

/// <summary>
/// Anonymous About probe — leaf AboutNode named mcp-function (no children).
/// </summary>
public class AboutFunction
{
	[Function(nameof(About))]
	public IActionResult About(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "about")] HttpRequest req)
	{
		return new OkObjectResult(AboutTreeBuilder.BuildMcpFunctionNode());
	}
}
