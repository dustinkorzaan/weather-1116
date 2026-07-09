using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

[ApiController]
[Route("[controller]")]
public class AboutController : ControllerBase
{
    [HttpGet]
    public ActionResult<AboutNode> Get()
    {
        var root = AboutTreeBuilder.BuildMvcRoot();
        return Ok(root);
    }
}
