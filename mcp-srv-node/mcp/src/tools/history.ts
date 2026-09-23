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
export const HISTORY_RESOLUTIONS = ['Daily', 'Hourly'] as const;
export type HistoryResolution = (typeof HISTORY_RESOLUTIONS)[number];

const RESOLUTION_QUERY: Record<HistoryResolution, Record<string, string>> = {
  Daily: {
    daily:
      'weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,wind_direction_10m_dominant',
    past_days: '7',
    forecast_days: '0',
  },
  Hourly: {
    hourly: 'temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m',
    past_hours: '48',
    forecast_hours: '0',
  },
};

export function buildHistoryUrl(latitude: number, longitude: number, resolution: string): string {
  if (!Object.hasOwn(RESOLUTION_QUERY, resolution)) {
    throw new Error(`Unsupported history resolution: '${resolution}'`);
  }
  return buildOpenMeteoUrl(latitude, longitude, RESOLUTION_QUERY[resolution as HistoryResolution]);
}

/**
 * Fetch recent past public weather for a latitude/longitude from Open-Meteo.
 * Daily is the previous 7 days, Hourly is the previous 48 hours.
 */
export async function getPublicWeatherHistory(
  latitude: number,
  longitude: number,
  resolution: HistoryResolution = 'Daily',
): Promise<OpenMeteoResponse> {
  const weatherData = await fetchOpenMeteo(buildHistoryUrl(latitude, longitude, resolution));

  normalizeSeriesBlock(weatherData.hourly as SeriesBlock | undefined, SUB_HOURLY_KEYS);
  normalizeSeriesBlock(weatherData.daily as SeriesBlock | undefined, DAILY_KEYS);

  return weatherData;
}
