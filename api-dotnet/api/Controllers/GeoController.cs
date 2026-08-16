using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class GeoController : ControllerBase
{
	private readonly IMediator _mediator;

	public GeoController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<ActionResult<NonAILatLongResponse>> Get(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(location))
		{
			return BadRequest();
		}

		try
		{
			var response = await _mediator.Send(
				new GetLatLongEvent
				{
					Location = location.Trim(),
					Count = 1,
				},
				cancellationToken);

			var first = response.Results.FirstOrDefault();
			if (first is null)
			{
				return NotFound();
			}

			return Ok(first);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpGet("GetLocation")]
	public async Task<ActionResult<NonAILocationResponse>> GetLocation(
		[FromQuery] double? latitude,
		[FromQuery] double? longitude,
		CancellationToken cancellationToken)
	{
		if (!IsValidCoordinate(latitude, longitude))
		{
			return BadRequest();
		}

		try
		{
			var response = await _mediator.Send(
				new GetLocationEvent
				{
					Latitude = latitude!.Value,
					Longitude = longitude!.Value,
				},
				cancellationToken);

			if (string.IsNullOrWhiteSpace(response.Location))
			{
				return NotFound();
			}

			return Ok(response);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	private static bool IsValidCoordinate(double? latitude, double? longitude) =>
		latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
}
