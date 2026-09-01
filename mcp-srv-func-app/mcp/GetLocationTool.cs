using Core.Geo.Events;
using Core.Geo.Models;
using CQMediator;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// MCP tool that reverse-geocodes lat/long to a simple place label via Core/CQMediator.
/// </summary>
public class GetLocationTool(IMediator mediator)
{
	[Function(nameof(GetLocation))]
	public async Task<NonAILocationResponse> GetLocation(
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
		return await mediator.Send(new GetLocationEvent
		{
			Latitude = latitude,
			Longitude = longitude,
		});
	}
}
