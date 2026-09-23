using System.ComponentModel;
using System.Text.Json;
using Core.Json;
using Core.Users.Events;
using CQMediator;
using Microsoft.Extensions.AI;

namespace Core.Tools;

/// <summary>
/// In-process Agent Framework versions of the user/pin tools (GetUser, AddUserPin, DeleteUserPin),
/// shared by Chat2a and by the User sub-agent in Chat4a/Chat5a. The Responses API loops use
/// <see cref="WeatherToolDefinitions"/> + <see cref="WeatherToolExecutor"/> instead, and the
/// remote-MCP paths call the same Core handlers through mcp-srv-app-service.
/// </summary>
public sealed class UserToolFunctions
{
    private readonly IMediator _mediator;

    public UserToolFunctions(IMediator mediator)
    {
        _mediator = mediator;
    }

    public IList<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(GetUser),
        AIFunctionFactory.Create(AddUserPin),
        AIFunctionFactory.Create(DeleteUserPin),
    ];

    [Description(WeatherToolDefinitions.GetUserDescription)]
    private async Task<string> GetUser(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserEvent(), cancellationToken);
        return JsonSerializer.Serialize(user, JsonDefaults.Pretty);
    }

    [Description(WeatherToolDefinitions.AddUserPinDescription)]
    private async Task<string> AddUserPin(
        [Description("Latitude in decimal degrees")] double latitude,
        [Description("Longitude in decimal degrees")] double longitude,
        [Description("Name of the location, e.g. Nashville, Tennessee")] string locationName,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddUserPinEvent
        {
            Latitude = latitude,
            Longitude = longitude,
            LocationName = locationName,
        }, cancellationToken);

        return JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
    }

    [Description(WeatherToolDefinitions.DeleteUserPinDescription)]
    private async Task<string> DeleteUserPin(
        [Description("The unique identifier (GUID) of the pin to delete, from GetUser")] Guid userPinId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserPinEvent { UserPinId = userPinId }, cancellationToken);

        return JsonSerializer.Serialize(new { success = true }, JsonDefaults.Pretty);
    }
}
