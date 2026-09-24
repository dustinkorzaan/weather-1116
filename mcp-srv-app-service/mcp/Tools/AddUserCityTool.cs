using System.ComponentModel;
using Core.Tools;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that adds a saved city for the current user via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class AddUserCityTool(IMediator mediator)
{
	[McpServerTool(Name = "AddUserCity"),
	 Description(WeatherToolDefinitions.AddUserCityDescription)]
	public async Task<UserCityToolResult> AddUserCity(
		[Description("Latitude in decimal degrees")] double latitude,
		[Description("Longitude in decimal degrees")] double longitude,
		[Description("Name of the location, e.g. Nashville, Tennessee")] string locationName,
		CancellationToken cancellationToken)
	{
		await mediator.Send(
			new AddUserCityEvent
			{
				Latitude = latitude,
				Longitude = longitude,
				LocationName = locationName,
			},
			cancellationToken);

		return new UserCityToolResult(true);
	}
}
