using System.ComponentModel;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that fetches public current weather via Core/MediatR.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherCurrentTool(IMediator mediator)
{
	[McpServerTool(Name = "GetPublicWeatherCurrent"),
	 Description("Get current public weather conditions for a latitude and longitude.")]
	public async Task<NonAIWeatherResponse> GetPublicWeatherCurrent(
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
