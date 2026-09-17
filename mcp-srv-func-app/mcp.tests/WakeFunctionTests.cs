using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMcpSrvFuncApp.Tests;

public class WakeFunctionTests
{
    [Fact]
    public void Wake_ReturnsOk()
    {
        var function = new WakeFunction();
        var context = new DefaultHttpContext();

        var result = function.Wake(context.Request);

        Assert.IsType<OkResult>(result);
    }
}
