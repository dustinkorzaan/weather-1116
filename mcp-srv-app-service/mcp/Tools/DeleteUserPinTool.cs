using System.ComponentModel;
using Core.Users.Events;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that removes a pin from the user's saved locations.
/// </summary>
[McpServerToolType]
public class DeleteUserPinTool(IMediator mediator)
{
	[McpServerTool(Name = "DeleteUserPin"),
	 Description("Remove a pin from the user's saved locations map.")]
	public async Task<dynamic> DeleteUserPin(
		[Description("The unique identifier (GUID) of the pin to delete")] string userPinId,
		CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(userPinId, out var pinId))
		{
			throw new ArgumentException("userPinId must be a valid GUID", nameof(userPinId));
		}

		return await mediator.Send(
			new DeleteUserPinEvent
			{
				UserPinId = pinId,
			},
			cancellationToken);
	}
}
