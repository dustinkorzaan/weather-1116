using System.Text.Json;
using Core.Weather.Models;

namespace Core.Tests.Weather.Models;

/// <summary>
/// Locks the JSON contract for the UI-facing weather responses. The Blazor, MVC, and React
/// clients all hardcode these camelCase property names independently, so a drift here would
/// break all three UIs without any of their own tests catching it.
/// </summary>
public class UIWeatherModelsTests
{
    [Fact]
    public void UIWeatherForecastResponse_SerializesCamelCaseContract()
    {
        var json = JsonSerializer.Serialize(new UIWeatherForecastResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new UIWeatherDailySeries
            {
                Time = ["2026-08-19"],
                WeatherCode = [1],
                TemperatureHighF = [88.4],
                TemperatureLowF = [70.1],
                PrecipitationInch = [0.3],
                WindSpeedMPH = [12.3],
                WindDirectionDegrees = [224],
            },
            Hourly = new UIWeatherHourlySeries
            {
                Time = ["2026-08-19T14:00"],
                TemperatureF = [86.5],
                PrecipitationInch = [0.0],
                WeatherCode = [1],
                WindSpeedMPH = [8.2],
                WindDirectionDegrees = [180],
            },
            Minutely15 = new UIWeatherHourlySeries
            {
                Time = ["2026-08-19T14:15"],
                TemperatureF = [86.7],
                PrecipitationInch = [0.0],
                WeatherCode = [1],
                WindSpeedMPH = [8.5],
                WindDirectionDegrees = [190],
            },
        });

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(36.16, root.GetProperty("latitude").GetDouble());
        Assert.Equal(-86.78, root.GetProperty("longitude").GetDouble());
        Assert.Equal("America/Chicago", root.GetProperty("timezone").GetString());

        var daily = root.GetProperty("daily");
        Assert.Equal(["2026-08-19"], daily.GetProperty("time").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(88.4, daily.GetProperty("temperatureHighF")[0].GetDouble());
        Assert.Equal(70.1, daily.GetProperty("temperatureLowF")[0].GetDouble());
        Assert.Equal(0.3, daily.GetProperty("precipitationInch")[0].GetDouble());
        Assert.Equal(12.3, daily.GetProperty("windSpeedMPH")[0].GetDouble());
        Assert.Equal(224, daily.GetProperty("windDirectionDegrees")[0].GetInt32());
        Assert.Equal(1, daily.GetProperty("weatherCode")[0].GetInt32());

        var hourly = root.GetProperty("hourly");
        Assert.Equal(86.5, hourly.GetProperty("temperatureF")[0].GetDouble());
        Assert.Equal(8.2, hourly.GetProperty("windSpeedMPH")[0].GetDouble());
        Assert.Equal(180, hourly.GetProperty("windDirectionDegrees")[0].GetInt32());

        var minutely15 = root.GetProperty("minutely15");
        Assert.Equal(86.7, minutely15.GetProperty("temperatureF")[0].GetDouble());
        Assert.Equal(8.5, minutely15.GetProperty("windSpeedMPH")[0].GetDouble());

        Assert.False(root.TryGetProperty("Minutely15", out _));
        Assert.False(root.TryGetProperty("minutely_15", out _));
        Assert.False(daily.TryGetProperty("temperature_2m_max", out _));
    }

    [Fact]
    public void UIWeatherHistoryResponse_SerializesCamelCaseContract()
    {
        var json = JsonSerializer.Serialize(new UIWeatherHistoryResponse
        {
            Latitude = 36.16,
            Longitude = -86.78,
            Timezone = "America/Chicago",
            Daily = new UIWeatherDailySeries
            {
                Time = ["2026-08-19"],
                WeatherCode = [1],
                TemperatureHighF = [88.4],
                TemperatureLowF = [70.1],
                PrecipitationInch = [0.3],
                WindSpeedMPH = [12.3],
                WindDirectionDegrees = [224],
            },
            Hourly = new UIWeatherHourlySeries
            {
                Time = ["2026-08-19T14:00"],
                TemperatureF = [86.5],
                PrecipitationInch = [0.0],
                WeatherCode = [1],
                WindSpeedMPH = [8.2],
                WindDirectionDegrees = [180],
            },
        });

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var daily = root.GetProperty("daily");
        Assert.Equal(88.4, daily.GetProperty("temperatureHighF")[0].GetDouble());
        Assert.Equal(70.1, daily.GetProperty("temperatureLowF")[0].GetDouble());
        Assert.Equal(0.3, daily.GetProperty("precipitationInch")[0].GetDouble());
        Assert.Equal(12.3, daily.GetProperty("windSpeedMPH")[0].GetDouble());

        var hourly = root.GetProperty("hourly");
        Assert.Equal(86.5, hourly.GetProperty("temperatureF")[0].GetDouble());
        Assert.Equal(0.0, hourly.GetProperty("precipitationInch")[0].GetDouble());
        Assert.Equal(8.2, hourly.GetProperty("windSpeedMPH")[0].GetDouble());
        Assert.Equal(180, hourly.GetProperty("windDirectionDegrees")[0].GetInt32());

        Assert.False(root.TryGetProperty("minutely15", out _));
    }
}
