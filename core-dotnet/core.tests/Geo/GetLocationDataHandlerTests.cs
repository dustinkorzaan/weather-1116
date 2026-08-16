using Core.Geo.Handlers;

namespace Core.Tests.Geo;

public class GetLocationDataHandlerTests
{
    [Fact]
    public void BuildReverseGeocodeUrl_UsesInvariantCoordinates()
    {
        var url = GetLocationDataHandler.BuildReverseGeocodeUrl(36.1627, -86.7816);

        Assert.StartsWith("https://nominatim.openstreetmap.org/reverse?", url);
        Assert.Contains("lat=36.1627", url);
        Assert.Contains("lon=-86.7816", url);
        Assert.Contains("zoom=10", url);
        Assert.Contains("accept-language=en", url);
        Assert.DoesNotContain(',', url);
    }
}
