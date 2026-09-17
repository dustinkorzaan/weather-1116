using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// Anonymous liveness probe for waking this Function App from zero. Unlike AboutFunction,
/// this does not scan the assembly for expected MCP tools, so callers that only need "is
/// this Function App up" (ui-react's wake screen) don't pay for that on every retry.
/// </summary>
public class WakeFunction
{
	[Function(nameof(Wake))]
	public IActionResult Wake(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Wake")] HttpRequest _)
	{
		return new OkResult();
	}
}
