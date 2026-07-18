using Core.currentweather;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class CurrentWeatherController : ControllerBase
{
    private readonly IMediator _mediator;

    public CurrentWeatherController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<CurrentWeatherConditions>> Get([FromQuery] string? location, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return BadRequest("location query parameter is required.");
        }

        var conditions = await _mediator.Send(new CurrentWeatherEvent { Location = location }, cancellationToken);
        return Ok(conditions);
    }
}
