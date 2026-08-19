using System.ComponentModel;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that fetches recent past public weather via Core/MediatR.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherHistoryTool(IMediator mediator)
{
	[McpServerTool(Name = "GetPublicWeatherHistory"),
	 Description("Get recent past public weather for a latitude and longitude. Daily is the previous 7 days, Hourly is the previous 48 hours. Use Daily unless the user asks for hourly detail.")]
	public async Task<NonAIHistoryWeatherResponse> GetPublicWeatherHistory(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		[Description("Daily (previous 7 days) or Hourly (previous 48 hours). Defaults to Daily.")]
		PublicWeatherHistoryResolution resolution = PublicWeatherHistoryResolution.Daily,
		CancellationToken cancellationToken = default)
	{
		return await mediator.Send(
			new GetPublicWeatherHistoryEvent
			{
				Latitude = latitude,
				Longitude = longitude,
				Resolution = resolution,
			},
			cancellationToken);
	}
}
