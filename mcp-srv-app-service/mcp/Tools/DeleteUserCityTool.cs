using System.ComponentModel;
using Core.Tools;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that deletes one of the current user's saved cities via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class DeleteUserCityTool(IMediator mediator)
{
	[McpServerTool(Name = "DeleteUserCity"),
	 Description(WeatherToolDefinitions.DeleteUserCityDescription)]
	public async Task<UserCityToolResult> DeleteUserCity(
		[Description("The unique identifier (GUID) of the saved city to delete, from GetUser")] Guid userCityId,
		CancellationToken cancellationToken)
	{
		await mediator.Send(new DeleteUserCityEvent { UserCityId = userCityId }, cancellationToken);

		return new UserCityToolResult(true);
	}
}
