using System.ComponentModel;
using Core.Tools;
using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using ModelContextProtocol.Server;

namespace WeatherMcpSrvAppService.Tools;

/// <summary>
/// MCP tool that returns the current user and their saved cities via Core/CQMediator.
/// </summary>
[McpServerToolType]
public class GetUserTool(IMediator mediator)
{
	[McpServerTool(Name = "GetUser"),
	 Description(WeatherToolDefinitions.GetUserDescription)]
	public async Task<UserDTO> GetUser(CancellationToken cancellationToken)
	{
		return await mediator.Send(new GetUserEvent(), cancellationToken);
	}
}
