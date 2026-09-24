using Core.Users.Events;
using Core.Users.Models;
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
        try
        {
            var user = await _mediator.Send(new GetUserEvent(), cancellationToken);
            return Ok(user);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost("AddCity")]
    public async Task<ActionResult> AddUserCity([FromBody] AddUserCityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new AddUserCityEvent
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationName = request.LocationName,
            }, cancellationToken);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("DeleteCity")]
    public async Task<ActionResult> DeleteUserCity([FromQuery] Guid userCityId, CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteUserCityEvent { UserCityId = userCityId }, cancellationToken);
            return Ok(new { success = true });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}

public class AddUserCityRequest
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public required string LocationName { get; set; }
}
