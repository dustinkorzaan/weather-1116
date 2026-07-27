using Core.About;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WeatherMcpFunction.Tests;

public class AboutFunctionTests
{
    [Fact]
    public void About_ReturnsHealthyMcpFunctionNode()
    {
        var function = new AboutFunction();
        var context = new DefaultHttpContext();

        var result = function.About(context.Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var node = Assert.IsType<AboutNode>(ok.Value);
        Assert.Equal("mcp-function", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void HasMcpTool_ReturnsTrue_ForGetLatLongData()
    {
        Assert.True(AboutFunction.HasMcpTool("GetLatLongData"));
    }

    [Fact]
    public void HasMcpTool_ReturnsFalse_ForUnknownTool()
    {
        Assert.False(AboutFunction.HasMcpTool("NotRegistered"));
    }
}
