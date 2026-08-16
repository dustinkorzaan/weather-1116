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
public class GetLocationDataTool(IMediator mediator, ILogger<GetLocationDataTool> logger)
{
	[Function(nameof(GetLocationData))]
	public async Task<string> GetLocationData(
		[McpToolTrigger(
			"GetLocationData",
			"Turn a latitude and longitude into a simple place label. US results are City, State; elsewhere City, State, Country.")]
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
		var result = await mediator.Send(new GetLocationDataEvent
		{
			Latitude = latitude,
			Longitude = longitude,
		});

		logger.LogInformation("MCP tool GetLocationData returned {Location}", result.Location);

		return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
	}
}
