using System.Text.Json;
using Core.Geo.Models;

namespace Core.Tests.Geo.Models;

/// <summary>
/// Verifies the JSON contracts the Core geo handlers rely on when deserializing
/// Open-Meteo geocoding responses and serializing ranked lat/long matches.
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
            Rank = 2,
            Name = "Paris",
            State = "Texas",
            Country = "United States",
            Latitude = 33.66,
            Longitude = -95.56,
        };

        var json = JsonSerializer.Serialize(original);
        Assert.Contains("\"rank\":2", json);
        Assert.Contains("\"name\":\"Paris\"", json);
        Assert.Contains("\"state\":\"Texas\"", json);
        Assert.Contains("\"country\":\"United States\"", json);
        Assert.DoesNotContain("admin1", json, StringComparison.OrdinalIgnoreCase);

        var roundTripped = JsonSerializer.Deserialize<NonAILatLongResponse>(json);
        Assert.NotNull(roundTripped);
        Assert.Equal(original.Rank, roundTripped!.Rank);
        Assert.Equal(original.Name, roundTripped.Name);
        Assert.Equal(original.State, roundTripped.State);
        Assert.Equal(original.Country, roundTripped.Country);
        Assert.Equal(original.Latitude, roundTripped.Latitude);
        Assert.Equal(original.Longitude, roundTripped.Longitude);
    }

    [Fact]
    public void NonAILatLongListResponse_RoundTripsResultsArray()
    {
        var original = new NonAILatLongListResponse
        {
            Results =
            [
                new NonAILatLongResponse { Rank = 1, Name = "Paris", State = "Île-de-France", Country = "France" },
                new NonAILatLongResponse { Rank = 2, Name = "Paris", State = "Texas", Country = "United States" },
            ],
        };

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<NonAILatLongListResponse>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped!.Results.Count);
        Assert.Equal(1, roundTripped.Results[0].Rank);
        Assert.Equal("Île-de-France", roundTripped.Results[0].State);
        Assert.Equal(2, roundTripped.Results[1].Rank);
        Assert.Equal("Texas", roundTripped.Results[1].State);
    }
}
