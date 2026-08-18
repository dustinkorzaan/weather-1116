using Core.Weather.Models;

namespace Core.Weather;

/// <summary>
/// Maps Open-Meteo's metric public weather responses (°C, km/h, mm) into the UI-facing
/// responses (°F, mph, in) so the UI only formats values instead of converting them.
/// Wind direction is passed through as meteorological source degrees (Open-Meteo convention).
/// </summary>
public static class WeatherResponseMapper
{
    public static UIWeatherForecastResponse ToUIForecastResponse(NonAIForecastWeatherResponse source) => new()
    {
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        Timezone = source.Timezone,
        Hourly = ToHourlySeries(source.Hourly),
        Daily = ToDailySeries(source.Daily),
        Minutely15 = ToHourlySeries(source.Minutely15),
    };

    public static UIWeatherHistoryResponse ToUIHistoryResponse(NonAIHistoryWeatherResponse source) => new()
    {
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        Timezone = source.Timezone,
        Hourly = ToHourlySeries(source.Hourly),
        Daily = ToDailySeries(source.Daily),
    };

    private static UIWeatherHourlySeries? ToHourlySeries(NonAIForecastWeatherHourly? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2mC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationMm.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10mKmh.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionSourceDegrees = [.. source.WindDirectionSource10m],
        };
    }

    private static UIWeatherHourlySeries? ToHourlySeries(NonAIForecastWeatherMinutely15? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2mC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationMm.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10mKmh.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionSourceDegrees = [.. source.WindDirectionSource10m],
        };
    }

    private static UIWeatherHourlySeries? ToHourlySeries(NonAIHistoryWeatherHourly? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherHourlySeries
        {
            Time = [.. source.Time],
            TemperatureF = source.Temperature2mC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationMm.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WeatherCode = [.. source.WeatherCode],
            WindSpeedMPH = source.WindSpeed10mKmh.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionSourceDegrees = [.. source.WindDirectionSource10m],
        };
    }

    private static UIWeatherDailySeries? ToDailySeries(NonAIForecastWeatherDaily? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherDailySeries
        {
            Time = [.. source.Time],
            WeatherCode = [.. source.WeatherCode],
            TemperatureHighF = source.Temperature2mMaxC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            TemperatureLowF = source.Temperature2mMinC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationSumMm.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WindSpeedMPH = source.WindSpeed10mMaxKmh.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionSourceDegrees = [.. source.WindDirectionSource10mDominant],
        };
    }

    private static UIWeatherDailySeries? ToDailySeries(NonAIHistoryWeatherDaily? source)
    {
        if (source is null)
        {
            return null;
        }

        return new UIWeatherDailySeries
        {
            Time = [.. source.Time],
            WeatherCode = [.. source.WeatherCode],
            TemperatureHighF = source.Temperature2mMaxC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            TemperatureLowF = source.Temperature2mMinC.ConvertAll(WeatherUnitConversion.CelsiusToFahrenheit),
            PrecipitationInch = source.PrecipitationSumMm.ConvertAll(WeatherUnitConversion.MillimetersToInches),
            WindSpeedMPH = source.WindSpeed10mMaxKmh.ConvertAll(WeatherUnitConversion.KilometersPerHourToMph),
            WindDirectionSourceDegrees = [.. source.WindDirectionSource10mDominant],
        };
    }
}
