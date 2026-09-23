import {
  buildOpenMeteoUrl,
  DAILY_KEYS,
  fetchOpenMeteo,
  normalizeSeriesBlock,
  type OpenMeteoResponse,
  type SeriesBlock,
  SUB_HOURLY_KEYS,
} from './openMeteo.ts';

// PascalCase to match the resolution values this tool has always exposed (mcp-srv-app-service,
// then mcp-srv-python), so callers/prompts tuned on the old tool keep working unchanged.
export const FORECAST_RESOLUTIONS = ['Daily', 'Hourly', 'FifteenMinutes'] as const;
export type ForecastResolution = (typeof FORECAST_RESOLUTIONS)[number];

const RESOLUTION_QUERY: Record<ForecastResolution, Record<string, string>> = {
  Daily: {
    daily:
      'weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant',
    forecast_days: '7',
  },
  Hourly: {
    hourly: 'temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m',
    forecast_hours: '48',
  },
  FifteenMinutes: {
    minutely_15: 'temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m',
    forecast_minutely_15: '192',
  },
};

export function buildForecastUrl(latitude: number, longitude: number, resolution: string): string {
  if (!Object.hasOwn(RESOLUTION_QUERY, resolution)) {
    throw new Error(`Unsupported forecast resolution: '${resolution}'`);
  }
  return buildOpenMeteoUrl(latitude, longitude, RESOLUTION_QUERY[resolution as ForecastResolution]);
}

/**
 * Fetch an upcoming public weather forecast for a latitude/longitude from Open-Meteo.
 * Daily is the next 7 days, Hourly is the next 48 hours, and FifteenMinutes is the next
 * 48 hours in 15-minute steps.
 */
export async function getPublicWeatherForecast(
  latitude: number,
  longitude: number,
  resolution: ForecastResolution = 'Daily',
): Promise<OpenMeteoResponse> {
  const weatherData = await fetchOpenMeteo(buildForecastUrl(latitude, longitude, resolution));

  normalizeSeriesBlock(weatherData.hourly as SeriesBlock | undefined, SUB_HOURLY_KEYS);
  normalizeSeriesBlock(weatherData.daily as SeriesBlock | undefined, DAILY_KEYS);
  normalizeSeriesBlock(weatherData.minutely_15 as SeriesBlock | undefined, SUB_HOURLY_KEYS);

  return weatherData;
}
