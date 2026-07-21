using System.Text.Json;
using Core.AIWeather.Events;
using Core.AIWeather.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AIWeatherController : ControllerBase
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

	[HttpGet("Current/stream")]
	public async Task GetCurrentStream(
		[FromQuery] string? location,
		CancellationToken cancellationToken)
	{
		Response.Headers.ContentType = "text/event-stream";
		Response.Headers.CacheControl = "no-cache";

		await foreach (var update in _mediator.CreateStream(
			new GetCurrentAIWeatherStreamEvent
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken))
		{
			var json = JsonSerializer.Serialize(update, JsonOptions);
			await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
			await Response.Body.FlushAsync(cancellationToken);
		}
	}
}
