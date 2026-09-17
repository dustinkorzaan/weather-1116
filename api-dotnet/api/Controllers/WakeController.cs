using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

/// <summary>
/// Anonymous liveness probe for waking this container from zero. Unlike AboutController,
/// this does not fan out to worker + both MCP hosts, so callers that only need "is this
/// container up" (ui-react's wake screen) don't pay for that fan-out on every retry.
/// </summary>
[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class WakeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
