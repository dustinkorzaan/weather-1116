export const OPEN_METEO_FORECAST_URL = 'https://api.open-meteo.com/v1/forecast';

// Pin all series to metric units; the caller can convert as needed.
const OPEN_METEO_UNITS = {
  temperature_unit: 'celsius',
  wind_speed_unit: 'kmh',
  precipitation_unit: 'mm',
};

export const SUB_HOURLY_KEYS = [
  'time',
  'temperature_2m',
  'precipitation',
  'weather_code',
  'wind_speed_10m',
  'wind_direction_10m',
] as const;

export const DAILY_KEYS = [
  'time',
  'weather_code',
  'temperature_2m_max',
  'temperature_2m_min',
  'precipitation_sum',
  'wind_speed_10m_max',
  'wind_direction_10m_dominant',
] as const;

const PRECIPITATION_KEYS = ['precipitation', 'precipitation_sum'];

export type SeriesBlock = Record<string, unknown>;
export type OpenMeteoResponse = Record<string, unknown>;

export function buildOpenMeteoUrl(latitude: number, longitude: number, resolutionQuery: Record<string, string>): string {
  const params = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude),
    ...resolutionQuery,
    ...OPEN_METEO_UNITS,
    timezone: 'auto',
  });
  return `${OPEN_METEO_FORECAST_URL}?${params.toString()}`;
}

/** Fill in any null series with an empty list and clamp negative precipitation readings to zero. */
export function normalizeSeriesBlock(block: SeriesBlock | null | undefined, keys: readonly string[]): void {
  if (block == null) {
    return;
  }

  // Open-Meteo can serialize a series field as JSON null instead of omitting it.
  for (const key of keys) {
    if (block[key] == null) {
      block[key] = [];
    }
  }

  for (const key of PRECIPITATION_KEYS) {
    const values = block[key];
    if (Array.isArray(values) && values.length > 0) {
      block[key] = values.map((value) => (typeof value === 'number' ? Math.max(value, 0) : value));
    }
  }
}

export async function fetchOpenMeteo(url: string): Promise<OpenMeteoResponse> {
  const response = await fetch(url, { signal: AbortSignal.timeout(30_000) });
  if (!response.ok) {
    throw new Error(`Open-Meteo request failed with HTTP ${response.status}.`);
  }
  return (await response.json()) as OpenMeteoResponse;
}
