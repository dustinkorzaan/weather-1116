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

	[HttpGet("CurrentV3")]
	public async Task<ActionResult<AIWeatherResponse>> GetCurrentV3(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		var response = await _mediator.Send(
			new GetCurrentAIWeatherV3Event
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken);

		return Ok(response);
	}

	[HttpGet("CurrentV4")]
	public async Task<ActionResult<AIWeatherResponse>> GetCurrentV4(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		var response = await _mediator.Send(
			new GetCurrentAIWeatherV4Event
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken);

		return Ok(response);
	}

	[HttpGet("CurrentV5")]
	public async Task<ActionResult<AIWeatherResponse>> GetCurrentV5(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		var response = await _mediator.Send(
			new GetCurrentAIWeatherV5Event
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken);

		return Ok(response);
	}
}
