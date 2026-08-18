const COMPASS_POINTS = [
  'N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE',
  'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW',
];

/** Converts meteorological degrees to a 16-point compass abbreviation. */
export function degreesToCompass(degrees) {
  const numeric = Number(degrees);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  const index = Math.round((((numeric % 360) + 360) % 360) / 22.5) % 16;
  return COMPASS_POINTS[index];
}

/** Formats an Open-Meteo daily date ("2026-08-19") or hourly timestamp as "Wed, Aug 19". */
export function formatCalendarDate(isoDate) {
  const value = String(isoDate ?? '');
  const date = new Date(value.includes('T') ? value : `${value}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return isoDate ?? '';
  }

  return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
}

/** Formats an Open-Meteo hourly/15-minute timestamp ("2026-08-19T14:00") as "2 PM" (minutes shown only when non-zero, e.g. "2:15 PM"). */
export function formatClockTime(isoDateTime) {
  const date = new Date(isoDateTime);
  if (Number.isNaN(date.getTime())) {
    return isoDateTime ?? '';
  }

  return date.toLocaleTimeString(undefined, {
    hour: 'numeric',
    ...(date.getMinutes() !== 0 ? { minute: '2-digit' } : {}),
  });
}

/** Formats an already-converted inches value (the API returns US customary units). */
export function formatPrecipitationIn(inches) {
  const numeric = Number(inches);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 100) / 100}"`;
}

/** Formats an already-converted °F value (the API returns US customary units). */
export function formatTemperatureF(fahrenheit) {
  const numeric = Number(fahrenheit);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 10) / 10} \u00B0F`;
}

/** Formats an already-converted mph value (the API returns US customary units). */
export function formatWindSpeedMph(mph) {
  const numeric = Number(mph);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 10) / 10} mph`;
}
