using Core.HelloWorld.Events;
using Core.HelloWorld.Models;
using CQMediator;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class HomeController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Hello")]
    public async Task<ActionResult<HelloWorldResponse>> Hello(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new HelloWorldEvent { Message = "from WeatherAPI" }, cancellationToken);
        return Ok(response);
    }
}
