using Core.Geo;
using Core.Geo.Models;

namespace Core.Tests.Geo;

public class NominatimLocationMapperTests
{
    [Fact]
    public void FromAddress_UsCityOmitsCountry()
    {
        var location = NominatimLocationMapper.FromAddress(new NominatimAddress
        {
            City = "Nashville",
            State = "Tennessee",
            Country = "United States",
            CountryCode = "us",
        });

        Assert.Equal("Nashville, Tennessee", location);
    }

    [Fact]
    public void FromAddress_NonUsIncludesCountry()
    {
        var location = NominatimLocationMapper.FromAddress(new NominatimAddress
        {
            City = "Paris",
            State = "Île-de-France",
            Country = "France",
            CountryCode = "fr",
        });

        Assert.Equal("Paris, Île-de-France, France", location);
    }

    [Theory]
    [InlineData("town", "Franklin")]
    [InlineData("village", "Bell Buckle")]
    [InlineData("municipality", "Metro Nashville")]
    [InlineData("county", "Davidson County")]
    public void FromAddress_UsesLocalityFallback(string field, string value)
    {
        var address = new NominatimAddress
        {
            State = "Tennessee",
            Country = "United States",
            CountryCode = "us",
        };

        switch (field)
        {
            case "town":
                address.Town = value;
                break;
            case "village":
                address.Village = value;
                break;
            case "municipality":
                address.Municipality = value;
                break;
            case "county":
                address.County = value;
                break;
        }

        Assert.Equal($"{value}, Tennessee", NominatimLocationMapper.FromAddress(address));
    }

    [Fact]
    public void FromAddress_PrefersCityOverTown()
    {
        var location = NominatimLocationMapper.FromAddress(new NominatimAddress
        {
            City = "Nashville",
            Town = "Franklin",
            State = "Tennessee",
            CountryCode = "us",
        });

        Assert.Equal("Nashville, Tennessee", location);
    }

    [Fact]
    public void FromAddress_SkipsBlankParts()
    {
        var location = NominatimLocationMapper.FromAddress(new NominatimAddress
        {
            City = "Singapore",
            Country = "Singapore",
            CountryCode = "sg",
        });

        Assert.Equal("Singapore, Singapore", location);
    }

    [Fact]
    public void FromAddress_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, NominatimLocationMapper.FromAddress(null));
        Assert.Equal(string.Empty, NominatimLocationMapper.FromAddress(new NominatimAddress()));
    }

    [Fact]
    public void FromReverse_PrefersStructuredLabelOverName()
    {
        var geoData = new NominatimReverseResponse
        {
            Name = "Davidson County",
            Address = new NominatimAddress
            {
                City = "Nashville",
                State = "Tennessee",
                CountryCode = "us",
            },
        };

        Assert.Equal("Nashville, Tennessee", NominatimLocationMapper.FromReverse(geoData, 36.16, -86.78));
    }

    [Fact]
    public void FromReverse_UsesNameWhenStructuredLabelIsEmpty()
    {
        var geoData = new NominatimReverseResponse { Name = "Gulf of Mexico" };

        Assert.Equal("Gulf of Mexico", NominatimLocationMapper.FromReverse(geoData, 25.0, -90.0));
    }

    [Fact]
    public void FromReverse_UsesFormattedCoordinatesWhenNameAndAddressAreEmpty()
    {
        Assert.Equal("35.51° N, 86.58° W", NominatimLocationMapper.FromReverse(null, 35.51, -86.58));
        Assert.Equal(
            "35.51° N, 86.58° W",
            NominatimLocationMapper.FromReverse(new NominatimReverseResponse(), 35.51, -86.58));
        Assert.Equal("33.87° S, 151.21° E", NominatimLocationMapper.FromReverse(null, -33.8688, 151.2093));
    }

    [Fact]
    public void FormatCoordinates_UsesTwoDecimalHemisphereLabels()
    {
        Assert.Equal("35.51° N, 86.58° W", NominatimLocationMapper.FormatCoordinates(35.51, -86.58));
        Assert.Equal("0.00° N, 0.00° E", NominatimLocationMapper.FormatCoordinates(0, 0));
    }
}
