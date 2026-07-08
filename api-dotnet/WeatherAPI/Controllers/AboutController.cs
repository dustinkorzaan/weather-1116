using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

/// <summary>
/// Returns the About health tree for the API app: "API Root" -> [API, Core Root -> Core].
/// </summary>
[ApiController]
[Route("[controller]")]
public class AboutController : ControllerBase
{
    [HttpGet]
    public ActionResult<AboutNode> Get()
    {
        var apiSelf = AboutNodeFactory.CreateSelfNode("API");
        var coreRoot = AboutNodeFactory.CreateCoreSubtree();
        var apiRoot = AboutNodeFactory.CreateRoot("API Root", apiSelf, coreRoot);

        return Ok(apiRoot);
    }
}
