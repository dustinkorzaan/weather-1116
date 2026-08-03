using System.Text.Json;
using Core.Geo.Models;

namespace Core.Tests.Geo.Models;

/// <summary>
/// Verifies the JSON contracts the Core geo handlers rely on when deserializing
/// Open-Meteo geocoding responses.
/// </summary>
public class GeoModelsTests
{
    [Fact]
    public void NonAIGeocodingResponse_DeserializesResultsArray()
    {
        const string json = """
        {
          "results": [
            {
              "name": "Nashville",
              "admin1": "Tennessee",
              "country": "United States",
              "latitude": 36.16589,
              "longitude": -86.78444
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<NonAIGeocodingResponse>(json);

        Assert.NotNull(result);
        var first = Assert.Single(result!.Results);
        Assert.Equal("Nashville", first.Name);
        Assert.Equal("Tennessee", first.Admin1);
        Assert.Equal("United States", first.Country);
        Assert.Equal(36.16589, first.Latitude);
        Assert.Equal(-86.78444, first.Longitude);
    }

    [Fact]
    public void NonAIGeocodingResponse_NoResultsKey_DefaultsToEmptyList()
    {
        var result = JsonSerializer.Deserialize<NonAIGeocodingResponse>("{}");

        Assert.NotNull(result);
        Assert.Empty(result!.Results);
    }

    [Fact]
    public void NonAILatLongResponse_RoundTripsThroughJson()
    {
        var original = new NonAILatLongResponse
        {
            Name = "Toronto",
            Latitude = 43.7,
            Longitude = -79.42,
        };

        var json = JsonSerializer.Serialize(original);
        Assert.Contains("\"name\":\"Toronto\"", json);
        Assert.Contains("\"latitude\":43.7", json);

        var roundTripped = JsonSerializer.Deserialize<NonAILatLongResponse>(json);
        Assert.NotNull(roundTripped);
        Assert.Equal(original.Name, roundTripped!.Name);
        Assert.Equal(original.Latitude, roundTripped.Latitude);
        Assert.Equal(original.Longitude, roundTripped.Longitude);
    }
}
