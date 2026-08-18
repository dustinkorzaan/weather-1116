using Core.Weather;
using Core.Weather.Models;

namespace Core.Tests.Weather;

public class WeatherResponseMapperTests
{
    [Fact]
    public void ToUIForecastResponse_ConvertsDailyHourlyAndMinutely15ToUSCustomaryUnits()
    {
        var source = new PublicWeatherForecastResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new PublicWeatherForecastDaily
            {
                Time = ["2026-08-16"],
                WeatherCode = [2],
                Temperature2mMax = [24],
                Temperature2mMin = [0],
                PrecipitationSum = [25.4],
                WindSpeed10mMax = [10],
                WindDirection10mDominant = [224],
            },
            Hourly = new PublicWeatherForecastHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2m = [24],
                Precipitation = [25.4],
                WeatherCode = [1],
                WindSpeed10m = [10],
                WindDirection10m = [180],
            },
            Minutely15 = new PublicWeatherForecastMinutely15
            {
                Time = ["2026-08-16T00:00"],
                Temperature2m = [0],
                Precipitation = [7.62],
                WeatherCode = [3],
                WindSpeed10m = [10],
                WindDirection10m = [90],
            },
        };

        var result = WeatherResponseMapper.ToUIForecastResponse(source);

        Assert.Equal(36.16, result.Latitude);
        Assert.Equal(-86.78, result.Longitude);
        Assert.Equal("America/Chicago", result.Timezone);

        Assert.NotNull(result.Daily);
        Assert.Equal([75.2], result.Daily!.TemperatureHighF);
        Assert.Equal([32], result.Daily.TemperatureLowF);
        Assert.Equal([1], result.Daily.PrecipitationInch);
        Assert.Equal([6.2], result.Daily.WindSpeedMPH);
        Assert.Equal([44], result.Daily.WindDirectionToDegrees);
        Assert.Equal([2], result.Daily.WeatherCode);

        Assert.NotNull(result.Hourly);
        Assert.Equal([75.2], result.Hourly!.TemperatureF);
        Assert.Equal([1], result.Hourly.PrecipitationInch);
        Assert.Equal([6.2], result.Hourly.WindSpeedMPH);
        Assert.Equal([0], result.Hourly.WindDirectionToDegrees);
        Assert.Equal([1], result.Hourly.WeatherCode);

        Assert.NotNull(result.Minutely15);
        Assert.Equal([32], result.Minutely15!.TemperatureF);
        Assert.Equal([0.3], result.Minutely15.PrecipitationInch);
        Assert.Equal([270], result.Minutely15.WindDirectionToDegrees);
    }

    [Fact]
    public void ToUIForecastResponse_MissingSeries_MapsToNull()
    {
        var result = WeatherResponseMapper.ToUIForecastResponse(new PublicWeatherForecastResponse());

        Assert.Null(result.Daily);
        Assert.Null(result.Hourly);
        Assert.Null(result.Minutely15);
    }

    [Fact]
    public void ToUIHistoryResponse_ConvertsDailyAndHourlyToUSCustomaryUnits()
    {
        var source = new PublicWeatherHistoryResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new PublicWeatherHistoryDaily
            {
                Time = ["2026-08-16"],
                WeatherCode = [2],
                Temperature2mMax = [24],
                Temperature2mMin = [0],
                PrecipitationSum = [25.4],
                WindSpeed10mMax = [10],
                WindDirection10mDominant = [224],
            },
            Hourly = new PublicWeatherHistoryHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2m = [24],
                Precipitation = [25.4],
                WeatherCode = [1],
                WindSpeed10m = [10],
                WindDirection10m = [180],
            },
        };

        var result = WeatherResponseMapper.ToUIHistoryResponse(source);

        Assert.NotNull(result.Daily);
        Assert.Equal([75.2], result.Daily!.TemperatureHighF);
        Assert.Equal([32], result.Daily.TemperatureLowF);
        Assert.Equal([1], result.Daily.PrecipitationInch);
        Assert.Equal([6.2], result.Daily.WindSpeedMPH);
        Assert.Equal([44], result.Daily.WindDirectionToDegrees);

        Assert.NotNull(result.Hourly);
        Assert.Equal([75.2], result.Hourly!.TemperatureF);
        Assert.Equal([1], result.Hourly.PrecipitationInch);
        Assert.Equal([6.2], result.Hourly.WindSpeedMPH);
        Assert.Equal([0], result.Hourly.WindDirectionToDegrees);
    }

    [Fact]
    public void ToUIHistoryResponse_MissingSeries_MapsToNull()
    {
        var result = WeatherResponseMapper.ToUIHistoryResponse(new PublicWeatherHistoryResponse());

        Assert.Null(result.Daily);
        Assert.Null(result.Hourly);
    }
}
