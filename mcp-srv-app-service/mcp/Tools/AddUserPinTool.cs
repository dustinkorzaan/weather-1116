using System.ComponentModel;
using Core.Tools;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that adds a saved map pin for the current user via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class AddUserPinTool(IMediator mediator)
{
	[McpServerTool(Name = "AddUserPin"),
	 Description(WeatherToolDefinitions.AddUserPinDescription)]
	public async Task<UserPinToolResult> AddUserPin(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		[Description("Name of the location, e.g. Nashville, Tennessee")] string locationName,
		CancellationToken cancellationToken)
	{
		await mediator.Send(
			new AddUserPinEvent
			{
				Latitude = latitude,
				Longitude = longitude,
				LocationName = locationName,
			},
			cancellationToken);

		return new UserPinToolResult(true);
	}
}
