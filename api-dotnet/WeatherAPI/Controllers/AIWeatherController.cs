using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AIWeatherController : ControllerBase
{
	private readonly IMediator _mediator;

	public AIWeatherController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet("Current")]
	public async Task<ActionResult<AIWeatherResponse>> GetCurrent(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		var response = await _mediator.Send(
			new GetCurrentAIWeatherEvent
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken);

		return Ok(response);
	}
}
