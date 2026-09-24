using Core.About;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace WeatherMcpSrvFuncApp.Tests;

public class AboutFunctionTests
{
    [Fact]
    public void About_ReturnsHealthyMcpSrvFuncAppNode_WhenDbConfigured()
    {
        var node = GetAboutNode(new Dictionary<string, string?> { ["DB_CONNECTION_STRING"] = "Server=db;" });

        Assert.Equal("mcp-srv-func-app", node.Name);
        Assert.True(node.IsHealthy);
        Assert.Empty(node.Children);
    }

    [Fact]
    public void About_ReportsUnhealthy_WithoutDbConnectionString()
    {
        var node = GetAboutNode([]);

        Assert.Equal("mcp-srv-func-app", node.Name);
        Assert.False(node.IsHealthy);
    }

    private static AboutNode GetAboutNode(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var function = new AboutFunction(configuration);
        var context = new DefaultHttpContext();

        var result = function.About(context.Request);

        var ok = Assert.IsType<OkObjectResult>(result);
        return Assert.IsType<AboutNode>(ok.Value);
    }

    [Fact]
    public void HasMcpTool_ReturnsTrue_ForGetCities()
    {
        Assert.True(AboutFunction.HasMcpTool("GetCities"));
    }

    [Theory]
    [InlineData("GetLatLong")]
    [InlineData("GetLocation")]
    public void HasMcpTool_ReturnsFalse_ForToolsMovedToMcpSrvPython(string toolName)
    {
        Assert.False(AboutFunction.HasMcpTool(toolName));
    }

    [Fact]
    public void HasMcpTool_ReturnsFalse_ForUnknownTool()
    {
        Assert.False(AboutFunction.HasMcpTool("NotRegistered"));
    }
}
