using Bunit;
using WeatherBlazor.Data;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

public sealed class AboutTreeNodeTests
{
    [Fact]
    public void RendersPublicMessage()
    {
        using var context = new BunitContext();
        var node = new AboutNode
        {
            Name = "Hangfire",
            PublicMessage = "0 failed, 1 processing, 2 enqueued",
            IsHealthy = true,
        };

        var rendered = context.Render<AboutTreeNode>(parameters =>
            parameters.Add(component => component.Node, node));

        Assert.Contains("0 failed, 1 processing, 2 enqueued", rendered.Markup);
    }
}
