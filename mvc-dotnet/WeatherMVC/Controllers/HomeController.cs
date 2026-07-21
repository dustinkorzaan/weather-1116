using System.Diagnostics;
using System.Text.Json;
using Core.AIWeather.Events;
using Core.demo.events;
using Core.demo.forecast;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WeatherMVC.Models;

namespace WeatherMVC.Controllers;

public class HomeController : Controller
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly IMediator _mediator;

	public HomeController(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<IActionResult> Index(CancellationToken cancellationToken)
	{
		var helloResponse = await _mediator.Send(new HelloWorldEvent { Message = "from WeatherMVC" }, cancellationToken);
		ViewData["HelloResponse"] = helloResponse.RequestResponse;

		var forecasts = await _mediator.Send(new WeatherForecastEvent(), cancellationToken);
		return View(forecasts);
	}

	[HttpGet]
	public async Task<IActionResult> GetCurrentAIWeather([FromQuery] string? location, CancellationToken cancellationToken)
	{
		var response = await _mediator.Send(
			new GetCurrentAIWeatherEvent
			{
				Location = string.IsNullOrWhiteSpace(location) ? "Nashville, TN" : location,
			},
			cancellationToken);

		return Json(response);
	}

	[HttpGet]
	public async Task GetCurrentAIWeatherStream([FromQuery] string? location, CancellationToken cancellationToken)
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
