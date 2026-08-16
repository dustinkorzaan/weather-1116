using Core.Geo;
using Core.Geo.Events;
using Core.Geo.Models;

namespace Core.Tests.Geo;

public class NonAILatLongMapperTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void NormalizeCount_ClampsToOpenMeteoRange(int requested, int expected)
    {
        Assert.Equal(expected, NonAILatLongMapper.NormalizeCount(requested));
    }

    [Fact]
    public void GetLatLongEvent_DefaultsCountToFive()
    {
        var request = new GetLatLongEvent { Location = "Paris" };

        Assert.Equal(5, request.Count);
        Assert.Equal(GetLatLongEvent.DefaultCount, request.Count);
    }

    [Fact]
    public void FromGeocodingResults_MapsAdmin1ToStateAndAssignsRanks()
    {
        var matches = new List<NonAIGeocodingResult>
        {
            new()
            {
                Name = "Paris",
                Admin1 = "Île-de-France",
                Country = "France",
                Latitude = 48.85,
                Longitude = 2.35,
            },
            new()
            {
                Name = "Paris",
                Admin1 = "Texas",
                Country = "United States",
                Latitude = 33.66,
                Longitude = -95.56,
            },
        };

        var mapped = NonAILatLongMapper.FromGeocodingResults(matches);

        Assert.Equal(2, mapped.Results.Count);

        Assert.Equal(1, mapped.Results[0].Rank);
        Assert.Equal("Paris", mapped.Results[0].Name);
        Assert.Equal("Île-de-France", mapped.Results[0].State);
        Assert.Equal("France", mapped.Results[0].Country);
        Assert.Equal(48.85, mapped.Results[0].Latitude);
        Assert.Equal(2.35, mapped.Results[0].Longitude);

        Assert.Equal(2, mapped.Results[1].Rank);
        Assert.Equal("Texas", mapped.Results[1].State);
        Assert.Equal("United States", mapped.Results[1].Country);
    }

    [Fact]
    public void FromGeocodingResults_EmptyInput_ReturnsEmptyResults()
    {
        var mapped = NonAILatLongMapper.FromGeocodingResults([]);

        Assert.Empty(mapped.Results);
    }
}
