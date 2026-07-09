using Core.demo.forecast;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IMediator _mediator;

    public WeatherForecastController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<WeatherForecast[]>> Get([FromQuery] DateTime? startDate, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new WeatherForecastEvent
        {
            StartDate = startDate.HasValue ? DateOnly.FromDateTime(startDate.Value) : null,
        }, cancellationToken);

        return Ok(response);
    }
}
