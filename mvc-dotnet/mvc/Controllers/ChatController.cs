using Microsoft.AspNetCore.Mvc;

namespace WeatherMVC.Controllers;

public class ChatController : Controller
{
    public IActionResult Index() => View();
}
