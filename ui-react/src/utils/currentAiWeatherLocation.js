export const CURRENT_AI_WEATHER_PATH = '/current-ai-weather';
export const LOCATION_QUERY_PARAM = 'location';

function formatHemisphereDegrees(value, positiveLabel, negativeLabel) {
  const numeric = Number(value);
  const hemisphere = numeric >= 0 ? positiveLabel : negativeLabel;
  return `${Math.abs(numeric).toFixed(4)}\u00B0 ${hemisphere}`;
}

/** Expands a map pin into "Nashville, TN (36.1659° N, 86.7844° W)". */
export function formatLocationWithLatLong(name, lat, lng) {
  const trimmed = (name ?? '').trim();
  if (!trimmed || !Number.isFinite(Number(lat)) || !Number.isFinite(Number(lng))) {
    return trimmed;
  }

  return `${trimmed} (${formatHemisphereDegrees(lat, 'N', 'S')}, ${formatHemisphereDegrees(lng, 'E', 'W')})`;
}

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
