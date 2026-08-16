export const CURRENT_AI_WEATHER_PATH = '/current-ai-weather';
export const LOCATION_QUERY_PARAM = 'location';

export function currentAiWeatherPath(location) {
  const trimmed = (location ?? '').trim();
  if (!trimmed) {
    return CURRENT_AI_WEATHER_PATH;
  }

  return `${CURRENT_AI_WEATHER_PATH}?${LOCATION_QUERY_PARAM}=${encodeURIComponent(trimmed)}`;
}

export function locationFromSearchParams(searchParams) {
  if (!searchParams) {
    return '';
  }

  return (searchParams.get(LOCATION_QUERY_PARAM) ?? '').trim();
}
