using System.Text.Json;
using Core.geo.Events;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace WeatherMcpFunction;

/// <summary>
/// MCP tool that resolves a location name to latitude/longitude via Core/MediatR.
/// </summary>
public class GetLatLongDataTool(IMediator mediator, ILogger<GetLatLongDataTool> logger)
{
	[Function(nameof(GetLatLongData))]
	public async Task<string> GetLatLongData(
		[McpToolTrigger(
			"GetLatLongData",
			"Resolve a location name to latitude and longitude using public geocoding data.")]
		ToolInvocationContext context,
		[McpToolProperty(
			"location",
			"City and optional region/country, e.g. Nashville, TN",
			true)]
		string location)
	{
		logger.LogInformation("MCP tool GetLatLongData invoked for location: {Location}", location);

		var result = await mediator.Send(new GetLatLongDataEvent { Location = location });

		return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
	}
}
