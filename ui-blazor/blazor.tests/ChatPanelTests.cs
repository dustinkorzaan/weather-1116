using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class ChatPanelTests
{
    [Theory]
    [InlineData("Chat1a", "1a")]
    [InlineData("Chat1b", "1b")]
    [InlineData("Chat2a", "2a")]
    [InlineData("Chat2b", "2b")]
    [InlineData("Chat3", "3")]
    public void TabShowsShortVisibleLabelUnderFullAccessibleName(string fullLabel, string shortLabel)
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton(new ChatApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }));

        var rendered = context.Render<WeatherBlazor.Shared.ChatPanel>();

        // Every tab carries the full Chat1a-style name as its accessible name and tooltip...
        Assert.Contains($"aria-label=\"{fullLabel}\"", rendered.Markup);
        Assert.Contains($"title=\"{fullLabel}\"", rendered.Markup);

        // ...while the text a sighted user sees inside the tab is the short form.
        var ariaLabelIndex = rendered.Markup.IndexOf($"aria-label=\"{fullLabel}\"", StringComparison.Ordinal);
        var openTagEnd = rendered.Markup.IndexOf('>', ariaLabelIndex) + 1;
        var closeTagStart = rendered.Markup.IndexOf("</fluent-tab>", openTagEnd, StringComparison.Ordinal);
        var innerText = rendered.Markup[openTagEnd..closeTagStart];

        Assert.Contains(shortLabel, innerText);
        Assert.DoesNotContain(fullLabel, innerText);
    }
}
