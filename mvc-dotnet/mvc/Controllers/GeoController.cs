using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

public class GeoController : Controller
{
	private readonly IMediator _mediator;

	public GeoController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> Index([FromQuery] string? location, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(location))
		{
			return BadRequest();
		}

		try
		{
			var response = await _mediator.Send(
				new GetLatLongDataEvent
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

			return Json(first);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}
}
