using Core.Users.Events;
using Core.Users.Models;
using CQMediator;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

[Route("[controller]")]
public class UserController : Controller
{
	private readonly IMediator _mediator;

	public UserController(IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet]
	public async Task<IActionResult> Index(CancellationToken cancellationToken)
	{
		try
		{
			var user = await _mediator.Send(new GetUserEvent(), cancellationToken);
			return Json(user);
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpPost("AddPin")]
	public async Task<IActionResult> AddPin([FromBody] AddUserPinRequest request, CancellationToken cancellationToken)
	{
		try
		{
			await _mediator.Send(new AddUserPinEvent
			{
				Latitude = request.Latitude,
				Longitude = request.Longitude,
				LocationName = request.LocationName,
			}, cancellationToken);
			return Json(new { success = true });
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}

	[HttpDelete("DeletePin")]
	public async Task<IActionResult> DeletePin([FromQuery] Guid userPinId, CancellationToken cancellationToken)
	{
		try
		{
			await _mediator.Send(new DeleteUserPinEvent { UserPinId = userPinId }, cancellationToken);
			return Json(new { success = true });
		}
		catch (InvalidOperationException)
		{
			return NotFound();
		}
	}
}

public class AddUserPinRequest
{
	public required double Latitude { get; set; }

	public required double Longitude { get; set; }

	public required string LocationName { get; set; }
}
