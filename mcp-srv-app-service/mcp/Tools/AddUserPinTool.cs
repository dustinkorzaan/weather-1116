using System.ComponentModel;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that adds a new pin to the user's saved locations.
/// </summary>
[McpServerToolType]
public class AddUserPinTool(IMediator mediator)
{
	[McpServerTool(Name = "AddUserPin"),
	 Description("Add a new pin to the user's saved locations map.")]
	public async Task<dynamic> AddUserPin(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		[Description("Name of the location, e.g. Nashville, Tennessee")] string locationName,
		CancellationToken cancellationToken)
	{
		return await mediator.Send(
			new AddUserPinEvent
			{
				Latitude = latitude,
				Longitude = longitude,
				LocationName = locationName,
			},
			cancellationToken);
	}
}
