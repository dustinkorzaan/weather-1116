using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AboutController : ControllerBase
{
    [HttpGet]
    public ActionResult<AboutNode> Get()
    {
        var root = AboutTreeBuilder.BuildApiRoot();
        return Ok(root);
    }
}
