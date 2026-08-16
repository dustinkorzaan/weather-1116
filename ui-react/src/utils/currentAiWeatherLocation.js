export const CURRENT_AI_WEATHER_PATH = '/current-ai-weather';
export const LOCATION_QUERY_PARAM = 'location';

function locationParts(location) {
  return String(location ?? '')
    .split(/[,\s]+/)
    .map((part) => part.trim())
    .filter(Boolean);
}

/** Split "Atlanta, GA" into a comma-free query value like "Atlanta GA". */
export function cleanLocationQuery(location) {
  return locationParts(location).join(' ');
}

/** Turn a query or pin label into a search value like "Atlanta, GA". */
export function locationSearchValue(location) {
  const parts = locationParts(location);
  if (parts.length >= 2 && parts[parts.length - 1].length === 2) {
    const state = parts[parts.length - 1].toUpperCase();
    return `${parts.slice(0, -1).join(' ')}, ${state}`;
  }

  return parts.join(' ');
}

export function currentAiWeatherPath(location) {
  const cleaned = cleanLocationQuery(location);
  if (!cleaned) {
    return CURRENT_AI_WEATHER_PATH;
  }

  return `${CURRENT_AI_WEATHER_PATH}?${LOCATION_QUERY_PARAM}=${encodeURIComponent(cleaned)}`;
}

export function locationFromSearchParams(searchParams) {
  if (!searchParams) {
    return '';
  }

  return locationSearchValue(searchParams.get(LOCATION_QUERY_PARAM));
}
