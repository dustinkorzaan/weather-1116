using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace WeatherBlazor.Tests;

public sealed class WindDirectionCellTests
{
    [Fact]
    public void RendersCompassLabelAndRotatesArrowBySourceDegrees()
    {
        using var context = new BunitContext();
        context.Services.AddFluentUIComponents();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var rendered = context.Render<WeatherBlazor.Shared.WindDirectionCell>(parameters =>
            parameters.Add(p => p.Compass, "SW").Add(p => p.Degrees, 224));

        Assert.Contains("SW (224°)", rendered.Markup);
        Assert.Contains("wind-direction-arrow", rendered.Markup);
        Assert.Contains("rotate(224deg)", rendered.Markup);
        Assert.Contains(">V</span>", rendered.Markup);
    }

    [Fact]
    public void NormalizesWraparoundDegreesForArrowRotation()
    {
        using var context = new BunitContext();
        context.Services.AddFluentUIComponents();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var rendered = context.Render<WeatherBlazor.Shared.WindDirectionCell>(parameters =>
            parameters.Add(p => p.Compass, "S").Add(p => p.Degrees, 540));

        Assert.Contains("S (180°)", rendered.Markup);
        Assert.Contains("rotate(180deg)", rendered.Markup);
    }
}
