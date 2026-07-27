using System.Reflection;
using Core.about;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherMcpFunction;

/// <summary>
/// Anonymous About probe — leaf AboutNode named mcp-function (no children).
/// </summary>
public class AboutFunction
{
	private const string ExpectedTool = "GetLatLongData";
	private static readonly Lazy<bool> HasExpectedTool = new(() => HasMcpTool(ExpectedTool));
	private static readonly JsonSerializerOptions AboutJsonOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	[Function(nameof(About))]
	public IActionResult About(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "about")] HttpRequest _)
	{
		var aboutNode = AboutTreeBuilder.BuildMcpFunctionNode(HasExpectedTool.Value);
		return new ContentResult
		{
			Content = JsonSerializer.Serialize(aboutNode, AboutJsonOptions),
			ContentType = "application/json",
			StatusCode = StatusCodes.Status200OK,
		};
	}

	/// <summary>
	/// The Functions MCP extension puts <see cref="McpToolTriggerAttribute"/> on a
	/// parameter (not the method), and does not expose an injectable tool list like
	/// mcp-dotnet's <c>IEnumerable&lt;McpServerTool&gt;</c>. Scanning this assembly is
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
