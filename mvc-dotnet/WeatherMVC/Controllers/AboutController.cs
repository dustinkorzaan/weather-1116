using Core.about;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

/// <summary>
/// Returns the About health tree for the MVC app: "MVC Root" -> [MVC].
/// </summary>
public class AboutController : Controller
{
    [HttpGet]
    public ActionResult<AboutNode> Index()
    {
        var mvcSelf = AboutNodeFactory.CreateSelfNode("MVC");
        var mvcRoot = AboutNodeFactory.CreateRoot("MVC Root", mvcSelf);

        return Json(mvcRoot);
    }
}
