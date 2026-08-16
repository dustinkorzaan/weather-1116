using System.Reflection;
using Core.About;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// Anonymous About probe — leaf AboutNode named mcp-srv-func-app (no children).
/// </summary>
public class AboutFunction
{
	private static readonly string[] ExpectedTools = ["GetLatLongData", "GetLocationData"];
	private static readonly Lazy<bool> HasExpectedTool = new(() =>
		ExpectedTools.All(HasMcpTool));

	[Function(nameof(About))]
	public IActionResult About(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "about")] HttpRequest _)
	{
		return new OkObjectResult(
			AboutTreeBuilder.BuildMcpSrvFuncAppNode(HasExpectedTool.Value));
	}

	/// <summary>
	/// The Functions MCP extension puts <see cref="McpToolTriggerAttribute"/> on a
	/// parameter (not the method), and does not expose an injectable tool list like
	/// mcp-srv-app-service's <c>IEnumerable&lt;McpServerTool&gt;</c>. Scanning this assembly is
	/// enough to confirm the expected tool is present.
	/// </summary>
	internal static bool HasMcpTool(string toolName)
	{
		foreach (var type in typeof(AboutFunction).Assembly.GetTypes())
		{
			foreach (var method in type.GetMethods(
				BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				foreach (var parameter in method.GetParameters())
				{
					var trigger = parameter.GetCustomAttribute<McpToolTriggerAttribute>();
					if (trigger is not null
						&& string.Equals(trigger.ToolName, toolName, StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
		}

		return false;
	}
}
