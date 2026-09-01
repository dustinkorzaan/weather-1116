using Core.Geo.Events;
using Core.Geo.Models;
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
	public async Task<NonAILatLongListResponse> GetLatLong(
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
		return await mediator.Send(new GetLatLongEvent { Location = location });
	}
}
