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

    [Route("hello-world")]
    [Route("Home/HelloWorld")]
    public async Task<IActionResult> HelloWorld(CancellationToken cancellationToken)
    {
        var helloResponse = await _mediator.Send(new HelloWorldEvent { Message = "from WeatherMVC" }, cancellationToken);
        ViewData["HelloResponse"] = helloResponse.RequestResponse;

        return View();
    }

    [Route("current-ai-weather")]
    [Route("Home/CurrentAIWeather")]
    public IActionResult CurrentAIWeather()
    {
        return View();
    }

    [Route("chat-clients")]
    [Route("Home/ChatClients")]
    public IActionResult ChatClients()
    {
        return View();
    }

    [Route("weather")]
    [Route("Home/Weather")]
    public IActionResult Weather([FromQuery] string? name, [FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] string? tab)
    {
        return View(WeatherModalViewModel.Create(name, lat, lng, tab));
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
