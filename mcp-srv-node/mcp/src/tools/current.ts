import { buildOpenMeteoUrl, fetchOpenMeteo, type OpenMeteoResponse } from './openMeteo.ts';

export function buildCurrentUrl(latitude: number, longitude: number): string {
  return buildOpenMeteoUrl(latitude, longitude, { current_weather: 'true' }, false);
}

/**
 * Fetch current public weather conditions for a latitude/longitude from Open-Meteo. Returns the raw
 * Open-Meteo payload (`current_weather` block plus units), the same shape mcp-srv-app-service's
 * GetPublicWeatherCurrent returned via Core's NonAICurrentWeatherResponse.
 */
export async function getPublicWeatherCurrent(latitude: number, longitude: number): Promise<OpenMeteoResponse> {
  return fetchOpenMeteo(buildCurrentUrl(latitude, longitude));
}
