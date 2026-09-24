namespace WeatherWorkerDotNet.Tests;

public class ProcessingJobKeepAliveTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildWakeUrl_OutsideContainerApps_ReturnsNull(string? hostname) =>
        Assert.Null(ProcessingJobKeepAlive.BuildWakeUrl(hostname));

    [Fact]
    public void BuildWakeUrl_UsesTheReplicaHostnameOverHttps() =>
        Assert.Equal(
            new Uri("https://wx1116-prod-worker--abc123.example.centralus.azurecontainerapps.io/Wake"),
            ProcessingJobKeepAlive.BuildWakeUrl("wx1116-prod-worker--abc123.example.centralus.azurecontainerapps.io"));

    [Fact]
    public void Interval_StaysInsideTheScaleInCooldown() =>
        Assert.True(ProcessingJobKeepAlive.Interval < TimeSpan.FromMinutes(30));
}
