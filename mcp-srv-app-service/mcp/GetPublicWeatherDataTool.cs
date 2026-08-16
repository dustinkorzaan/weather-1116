using System.ComponentModel;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService;

/// <summary>
/// MCP tool that fetches public current weather via Core/MediatR.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherDataTool(IMediator mediator)
{
	[McpServerTool(Name = "GetPublicWeatherData"),
	 Description("Get current public weather conditions for a latitude and longitude.")]
	public async Task<NonAIWeatherResponse> GetPublicWeatherData(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		CancellationToken cancellationToken)
	{
		return await mediator.Send(
			new GetPublicWeatherDataEvent
			{
				Latitude = latitude,
				Longitude = longitude,
			},
			cancellationToken);
	}
}
