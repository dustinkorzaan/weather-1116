using Core.Weather;
using Core.Weather.Models;

namespace Core.Tests.Weather;

public class WeatherResponseMapperTests
{
    [Fact]
    public void ToUIForecastResponse_ConvertsDailyHourlyAndMinutely15ToUSCustomaryUnits()
    {
        var source = new NonAIForecastWeatherResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new NonAIForecastWeatherDaily
            {
                Time = ["2026-08-16"],
                WeatherCode = [2],
                Temperature2mMaxC = [24],
                Temperature2mMinC = [0],
                PrecipitationSumMm = [25.4],
                WindSpeed10mMaxKmh = [10],
                WindDirectionSource10mDominant = [224],
            },
            Hourly = new NonAIForecastWeatherHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2mC = [24],
                PrecipitationMm = [25.4],
                WeatherCode = [1],
                WindSpeed10mKmh = [10],
                WindDirectionSource10m = [180],
            },
            Minutely15 = new NonAIForecastWeatherMinutely15
            {
                Time = ["2026-08-16T00:00"],
                Temperature2mC = [0],
                PrecipitationMm = [7.62],
                WeatherCode = [3],
                WindSpeed10mKmh = [10],
                WindDirectionSource10m = [90],
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
        Assert.Equal([224], result.Daily.WindDirectionSourceDegrees);
        Assert.Equal(["SW"], result.Daily.WindDirectionSource);
        Assert.Equal([2], result.Daily.WeatherCode);

        Assert.NotNull(result.Hourly);
        Assert.Equal([75.2], result.Hourly!.TemperatureF);
        Assert.Equal([1], result.Hourly.PrecipitationInch);
        Assert.Equal([6.2], result.Hourly.WindSpeedMPH);
        Assert.Equal([180], result.Hourly.WindDirectionSourceDegrees);
        Assert.Equal(["S"], result.Hourly.WindDirectionSource);
        Assert.Equal([1], result.Hourly.WeatherCode);

        Assert.NotNull(result.Minutely15);
        Assert.Equal([32], result.Minutely15!.TemperatureF);
        Assert.Equal([0.3], result.Minutely15.PrecipitationInch);
        Assert.Equal([90], result.Minutely15.WindDirectionSourceDegrees);
        Assert.Equal(["E"], result.Minutely15.WindDirectionSource);
    }

    [Fact]
    public void ToUIForecastResponse_MissingSeries_MapsToNull()
    {
        var result = WeatherResponseMapper.ToUIForecastResponse(new NonAIForecastWeatherResponse());

        Assert.Null(result.Daily);
        Assert.Null(result.Hourly);
        Assert.Null(result.Minutely15);
    }

    [Fact]
    public void ToUIHistoryResponse_ConvertsDailyAndHourlyToUSCustomaryUnits()
    {
        var source = new NonAIHistoryWeatherResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new NonAIHistoryWeatherDaily
            {
                Time = ["2026-08-16"],
                WeatherCode = [2],
                Temperature2mMaxC = [24],
                Temperature2mMinC = [0],
                PrecipitationSumMm = [25.4],
                WindSpeed10mMaxKmh = [10],
                WindDirectionSource10mDominant = [224],
            },
            Hourly = new NonAIHistoryWeatherHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2mC = [24],
                PrecipitationMm = [25.4],
                WeatherCode = [1],
                WindSpeed10mKmh = [10],
                WindDirectionSource10m = [180],
            },
        };

        var result = WeatherResponseMapper.ToUIHistoryResponse(source);

        Assert.NotNull(result.Daily);
        Assert.Equal([75.2], result.Daily!.TemperatureHighF);
        Assert.Equal([32], result.Daily.TemperatureLowF);
        Assert.Equal([1], result.Daily.PrecipitationInch);
        Assert.Equal([6.2], result.Daily.WindSpeedMPH);
        Assert.Equal([224], result.Daily.WindDirectionSourceDegrees);
        Assert.Equal(["SW"], result.Daily.WindDirectionSource);

        Assert.NotNull(result.Hourly);
        Assert.Equal([75.2], result.Hourly!.TemperatureF);
        Assert.Equal([1], result.Hourly.PrecipitationInch);
        Assert.Equal([6.2], result.Hourly.WindSpeedMPH);
        Assert.Equal([180], result.Hourly.WindDirectionSourceDegrees);
        Assert.Equal(["S"], result.Hourly.WindDirectionSource);
    }

    [Fact]
    public void ToUIForecastResponse_NormalizesWindDirectionSourceDegrees()
    {
        var source = new NonAIForecastWeatherResponse
        {
            Hourly = new NonAIForecastWeatherHourly
            {
                Time = ["2026-08-16T00:00"],
                Temperature2mC = [0],
                PrecipitationMm = [0],
                WeatherCode = [0],
                WindSpeed10mKmh = [0],
                WindDirectionSource10m = [-90, 360, 450],
            },
        };

        var result = WeatherResponseMapper.ToUIForecastResponse(source);

        Assert.Equal([270, 0, 90], result.Hourly!.WindDirectionSourceDegrees);
        Assert.Equal(["W", "N", "E"], result.Hourly.WindDirectionSource);
    }

    [Fact]
    public void ToUIHistoryResponse_MissingSeries_MapsToNull()
    {
        var result = WeatherResponseMapper.ToUIHistoryResponse(new NonAIHistoryWeatherResponse());

        Assert.Null(result.Daily);
        Assert.Null(result.Hourly);
    }
}
