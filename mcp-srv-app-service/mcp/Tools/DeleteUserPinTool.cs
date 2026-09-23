using System.ComponentModel;
using Core.Tools;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that deletes one of the current user's saved map pins via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class DeleteUserPinTool(IMediator mediator)
{
	[McpServerTool(Name = "DeleteUserPin"),
	 Description(WeatherToolDefinitions.DeleteUserPinDescription)]
	public async Task<UserPinToolResult> DeleteUserPin(
		[Description("The unique identifier (GUID) of the pin to delete, from GetUser")] Guid userPinId,
		CancellationToken cancellationToken)
	{
		await mediator.Send(new DeleteUserPinEvent { UserPinId = userPinId }, cancellationToken);

		return new UserPinToolResult(true);
	}
}
