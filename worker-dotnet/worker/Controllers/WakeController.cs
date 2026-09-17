using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeatherWorkerDotNet.Controllers;

/// <summary>
/// Anonymous liveness probe for waking this container from zero. Unlike AboutController,
/// this does not touch Hangfire's monitoring API, so callers that only need "is this
/// container up" (ui-react's wake screen) don't pay for that on every retry.
/// </summary>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public sealed class WakeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
