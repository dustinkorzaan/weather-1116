using System.Diagnostics;
using Core.AIWeather.Events;
using Core.HelloWorld.Events;
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

    public IActionResult Index()
    {
        return View();
    }

    [Route("presentation")]
    [Route("Home/Presentation")]
    public async Task<IActionResult> Presentation(CancellationToken cancellationToken)
    {
        var helloResponse = await _mediator.Send(new HelloWorldEvent { Message = "from WeatherMVC" }, cancellationToken);
        ViewData["HelloResponse"] = helloResponse.RequestResponse;

        return View();
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
