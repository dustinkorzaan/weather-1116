using System.Diagnostics;
using System.Text.Json;
using Core.Geo.Events;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;

namespace WeatherMcpSrvFuncApp;

/// <summary>
/// MCP tool that resolves a location name to ranked latitude/longitude matches via Core/MediatR.
/// </summary>
public class GetLatLongTool(IMediator mediator, ILogger<GetLatLongTool> logger)
{
	[Function(nameof(GetLatLong))]
	public async Task<string> GetLatLong(
		[McpToolTrigger(
			"GetLatLong",
			"Resolve a location name to ranked latitude/longitude matches using public geocoding data. Returns up to 5 results (rank 1 is the best match). Use state and country to pick the right place if rank 1 is wrong.")]
		ToolInvocationContext context,
		[McpToolProperty(
			"location",
			"City and optional region/country, e.g. Nashville, TN",
			true)]
		string location)
	{
		logger.LogInformation("MCP tool GetLatLong invoked for location: {Location} at {Timestamp:o}", location, DateTimeOffset.UtcNow);

		var stopwatch = Stopwatch.StartNew();
		var result = await mediator.Send(new GetLatLongEvent { Location = location });
		stopwatch.Stop();

		logger.LogInformation("MCP tool GetLatLong completed for {Location} in {ElapsedMs}ms", location, stopwatch.ElapsedMilliseconds);

		return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
	}
}
