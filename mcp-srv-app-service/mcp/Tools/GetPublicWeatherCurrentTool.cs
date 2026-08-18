using System.ComponentModel;
using System.Diagnostics;
using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that fetches public current weather via Core/MediatR.
/// </summary>
[McpServerToolType]
public class GetPublicWeatherCurrentTool(IMediator mediator, ILogger<GetPublicWeatherCurrentTool> logger)
{
	[McpServerTool(Name = "GetPublicWeatherCurrent"),
	 Description("Get current public weather conditions for a latitude and longitude.")]
	public async Task<NonAIWeatherResponse> GetPublicWeatherCurrent(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		CancellationToken cancellationToken)
	{
		logger.LogInformation(
			"MCP tool GetPublicWeatherCurrent invoked for {Latitude},{Longitude} at {Timestamp:o}",
			latitude,
			longitude,
			DateTimeOffset.UtcNow);

		var stopwatch = Stopwatch.StartNew();
		var result = await mediator.Send(
			new GetPublicWeatherCurrentEvent
			{
				Latitude = latitude,
				Longitude = longitude,
			},
			cancellationToken);
		stopwatch.Stop();

		logger.LogInformation(
			"MCP tool GetPublicWeatherCurrent completed for {Latitude},{Longitude} in {ElapsedMs}ms",
			latitude,
			longitude,
			stopwatch.ElapsedMilliseconds);

		return result;
	}
}
