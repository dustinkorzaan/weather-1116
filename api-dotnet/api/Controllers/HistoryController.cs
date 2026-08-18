using Core.Weather.Events;
using Core.Weather.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class HistoryController : ControllerBase
{
	private readonly IMediator _mediator;

	public HistoryController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<ActionResult<PublicWeatherHistoryResponse>> Get(
		[FromQuery] double? latitude,
		[FromQuery] double? longitude,
		[FromQuery] PublicWeatherHistoryResolution resolution = PublicWeatherHistoryResolution.Daily,
		CancellationToken cancellationToken = default)
	{
		if (!IsValidCoordinate(latitude, longitude))
		{
			return BadRequest();
		}

		try
		{
			var response = await _mediator.Send(
				new GetPublicWeatherHistoryEvent
				{
					Latitude = latitude!.Value,
					Longitude = longitude!.Value,
					Resolution = resolution,
				},
				cancellationToken);

			return Ok(response);
		}
		catch (InvalidOperationException)
		{
			return StatusCode(StatusCodes.Status502BadGateway);
		}
	}

	private static bool IsValidCoordinate(double? latitude, double? longitude) =>
		latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
}
