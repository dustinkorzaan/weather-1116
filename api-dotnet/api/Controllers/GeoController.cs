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
}
