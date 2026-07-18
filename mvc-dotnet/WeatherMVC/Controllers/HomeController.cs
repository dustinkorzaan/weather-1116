using System.Diagnostics;
using Core.currentweather;
using Core.demo.events;
using Core.demo.forecast;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WeatherMVC.Models;

namespace WeatherMVC.Controllers;

public class HomeController : Controller
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public HomeController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var helloResponse = await _mediator.Send(new HelloWorldEvent { Message = "from WeatherMVC" }, cancellationToken);
        ViewData["HelloResponse"] = helloResponse.RequestResponse;

        var defaultLocation = _configuration["CurrentWeather:DefaultLocation"] ?? "New York, NY";
        try
        {
            var conditions = await _mediator.Send(new CurrentWeatherEvent { Location = defaultLocation }, cancellationToken);
            ViewData["CurrentWeather"] = conditions;
        }
        catch
        {
            // Non-fatal: current weather is best-effort.
        }

        var forecasts = await _mediator.Send(new WeatherForecastEvent(), cancellationToken);
        return View(forecasts);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
