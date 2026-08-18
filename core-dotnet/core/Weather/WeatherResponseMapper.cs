using Core.Weather.Models;

namespace Core.Weather;

/// <summary>
/// Maps Open-Meteo's metric public weather responses (°C, km/h, mm) into the UI-facing
/// responses (°F, mph, in) so the UI only formats values instead of converting them.
/// Wind direction is converted from meteorological from-degrees to towards heading.
/// </summary>
public static class WeatherResponseMapper
{
    public static UIWeatherForecastResponse ToUIForecastResponse(PublicWeatherForecastResponse source) => new()
    {
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        Timezone = source.Timezone,
        Hourly = ToHourlySeries(source.Hourly),
        Daily = ToDailySeries(source.Daily),
        Minutely15 = ToHourlySeries(source.Minutely15),
    };

    public static UIWeatherHistoryResponse ToUIHistoryResponse(PublicWeatherHistoryResponse source) => new()
    {
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        Timezone = source.Timezone,
        Hourly = ToHourlySeries(source.Hourly),
        Daily = ToDailySeries(source.Daily),
    };

    private static UIWeatherHourlySeries? ToHourlySeries(PublicWeatherForecastHourly? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2m.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.Precipitation.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10m.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionTowardsDegrees = source.WindDirection10m.ConvertAll(WeatherUnitConversion.MeteorologicalFromToWindTowards),
        };
    }

    private static UIWeatherHourlySeries? ToHourlySeries(PublicWeatherForecastMinutely15? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2m.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.Precipitation.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10m.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionTowardsDegrees = source.WindDirection10m.ConvertAll(WeatherUnitConversion.MeteorologicalFromToWindTowards),
        };
    }

    private static UIWeatherHourlySeries? ToHourlySeries(PublicWeatherHistoryHourly? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2m.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.Precipitation.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10m.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionTowardsDegrees = source.WindDirection10m.ConvertAll(WeatherUnitConversion.MeteorologicalFromToWindTowards),
        };
    }

    private static UIWeatherDailySeries? ToDailySeries(PublicWeatherForecastDaily? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherDailySeries
        {
            Time = [.. source.Time],
            WeatherCode = [.. source.WeatherCode],
            TemperatureHighF = source.Temperature2mMax.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            TemperatureLowF = source.Temperature2mMin.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationSum.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WindSpeedMPH = source.WindSpeed10mMax.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionTowardsDegrees = source.WindDirection10mDominant.ConvertAll(WeatherUnitConversion.MeteorologicalFromToWindTowards),
        };
    }

    private static UIWeatherDailySeries? ToDailySeries(PublicWeatherHistoryDaily? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherDailySeries
        {
            Time = [.. source.Time],
            WeatherCode = [.. source.WeatherCode],
            TemperatureHighF = source.Temperature2mMax.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            TemperatureLowF = source.Temperature2mMin.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationSum.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WindSpeedMPH = source.WindSpeed10mMax.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionTowardsDegrees = source.WindDirection10mDominant.ConvertAll(WeatherUnitConversion.MeteorologicalFromToWindTowards),
        };
    }
}
