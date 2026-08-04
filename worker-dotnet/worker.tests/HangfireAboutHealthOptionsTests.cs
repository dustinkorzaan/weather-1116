using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherWorkerDotNet;

namespace WeatherWorkerDotNet.Tests;

public class HangfireAboutHealthOptionsTests
{
    [Fact]
    public void Configure_UsesDefaultThresholds_WhenNotConfigured()
    {
        var options = new HangfireAboutHealthOptions();
        HangfireAboutHealthOptions.Configure(options, new ConfigurationBuilder().Build());

        Assert.Equal(30, options.StaleProcessingMinutes);
        Assert.Equal(60, options.StaleEnqueuedMinutes);
    }

    [Fact]
    public void Configure_UsesConfiguredThresholds()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HangfireAboutHealth_StaleProcessingMinutes"] = "15",
                ["HangfireAboutHealth_StaleEnqueuedMinutes"] = "45",
            })
            .Build();

        var options = new HangfireAboutHealthOptions();
        HangfireAboutHealthOptions.Configure(options, configuration);

        Assert.Equal(15, options.StaleProcessingMinutes);
        Assert.Equal(45, options.StaleEnqueuedMinutes);
    }

    [Fact]
    public void DependencyInjection_RegistersDefaultThresholds()
    {
        var services = new ServiceCollection();
        services.Configure<HangfireAboutHealthOptions>(options =>
            HangfireAboutHealthOptions.Configure(options, new ConfigurationBuilder().Build()));

        var resolved = services.BuildServiceProvider()
            .GetRequiredService<IOptions<HangfireAboutHealthOptions>>()
            .Value;

        Assert.Equal(30, resolved.StaleProcessingMinutes);
        Assert.Equal(60, resolved.StaleEnqueuedMinutes);
    }
}
