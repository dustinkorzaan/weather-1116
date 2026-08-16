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
}
