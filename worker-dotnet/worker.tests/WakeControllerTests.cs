using Microsoft.AspNetCore.Mvc;
using WeatherWorkerDotNet.Controllers;

namespace WeatherWorkerDotNet.Tests;

public class WakeControllerTests
{
    [Fact]
    public void Get_ReturnsOk()
    {
        var controller = new WakeController();

        var result = controller.Get();

        Assert.IsType<OkResult>(result);
    }
}
