using System.Diagnostics;
using Core.demo.events;
using Core.demo.forecast;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WeatherMVC.Models;

namespace WeatherMVC.Controllers;

public class HomeController : Controller
{
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
