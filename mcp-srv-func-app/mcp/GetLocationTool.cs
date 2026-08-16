using System.Text.Json;
using Core.Geo.Events;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// MCP tool that reverse-geocodes lat/long to a simple place label via Core/MediatR.
/// </summary>
public class GetLocationTool(IMediator mediator, ILogger<GetLocationTool> logger)
{
	[Function(nameof(GetLocation))]
	public async Task<string> GetLocation(
		[McpToolTrigger(
			"GetLocation",
			"Turn a latitude and longitude into a simple place label. Prefers City, State in the US (City, State, Country elsewhere), then a feature name, then a formatted coordinate such as 35.51° N, 86.58° W.")]
		ToolInvocationContext context,
		[McpToolProperty(
			"latitude",
			"Latitude in decimal degrees",
			true)]
		double latitude,
		[McpToolProperty(
			"longitude",
			"Longitude in decimal degrees",
			true)]
		double longitude)
	{
		var result = await mediator.Send(new GetLocationEvent
		{
			Latitude = latitude,
			Longitude = longitude,
		});

		logger.LogInformation("MCP tool GetLocation returned {Location}", result.Location);

		return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
	}
}
