using System.ComponentModel;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that fetches an upcoming public weather forecast via Core/MediatR.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherForecastTool(IMediator mediator)
{
	[McpServerTool(Name = "GetPublicWeatherForecast"),
	 Description("Get an upcoming public weather forecast for a latitude and longitude. Daily is the next 7 days, Hourly is the next 48 hours, and FifteenMinutes is the next 48 hours in 15-minute steps. Use Daily unless the user asks for hourly or 15-minute detail.")]
	public async Task<NonAIForecastWeatherResponse> GetPublicWeatherForecast(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		[Description("Daily (next 7 days), Hourly (next 48 hours), or FifteenMinutes (next 48 hours). Defaults to Daily.")]
		PublicWeatherForecastResolution resolution = PublicWeatherForecastResolution.Daily,
		CancellationToken cancellationToken = default)
	{
		return await mediator.Send(
			new GetPublicWeatherForecastEvent
			{
				Latitude = latitude,
				Longitude = longitude,
				Resolution = resolution,
			},
			cancellationToken);
	}
}
