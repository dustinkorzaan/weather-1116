using Core.User.Events;
using Core.User.Models;
using CQMediator;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<UserDTO>> GetUser(CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserEvent(), cancellationToken);
        return Ok(user);
    }

    [HttpPost("AddPin")]
    public async Task<ActionResult<UserDTO>> AddUserPin([FromBody] AddUserPinRequest request, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new AddUserPinEvent
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationName = request.LocationName,
        }, cancellationToken);
        return Ok(user);
    }

    [HttpDelete("DeletePin")]
    public async Task<ActionResult<UserDTO>> DeleteUserPin([FromQuery] Guid userPinId, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new DeleteUserPinEvent { UserPinId = userPinId }, cancellationToken);
        return Ok(user);
    }
}

public class AddUserPinRequest
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public required string LocationName { get; set; }
}
