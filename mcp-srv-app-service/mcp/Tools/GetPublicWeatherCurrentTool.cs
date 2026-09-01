using System.ComponentModel;
using Core.Weather.Events;
using Core.Weather.Models;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that fetches public current weather via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherCurrentTool(IMediator mediator)
{
	[McpServerTool(Name = "GetPublicWeatherCurrent", UseStructuredContent = true),
	 Description("Get current public weather conditions for a latitude and longitude.")]
	public async Task<NonAICurrentWeatherResponse> GetPublicWeatherCurrent(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		CancellationToken cancellationToken)
	{
		return await mediator.Send(
			new GetPublicWeatherCurrentEvent
			{
				Latitude = latitude,
				Longitude = longitude,
			},
			cancellationToken);
	}
}
