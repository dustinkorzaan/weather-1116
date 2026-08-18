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

/** Formats an Open-Meteo daily date ("2026-08-19") as "Wed, Aug 19". */
export function formatCalendarDate(isoDate) {
  const date = new Date(`${isoDate}T00:00:00`);
  if (Number.isNaN(date.getTime())) {
    return isoDate ?? '';
  }

  return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
}

/** Formats an Open-Meteo hourly/15-minute timestamp ("2026-08-19T14:00") as "Wed, Aug 19, 2 PM" (minutes shown only when non-zero, e.g. "2:15 PM"). */
export function formatClockTime(isoDateTime) {
  const date = new Date(isoDateTime);
  if (Number.isNaN(date.getTime())) {
    return isoDateTime ?? '';
  }

  const datePart = date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  const timePart = date.toLocaleTimeString(undefined, {
    hour: 'numeric',
    ...(date.getMinutes() !== 0 ? { minute: '2-digit' } : {}),
  });

  return `${datePart}, ${timePart}`;
}

/** Converts Open-Meteo mm to inches, then formats. */
export function formatPrecipitationIn(millimeters) {
  const numeric = Number(millimeters);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round((numeric / 25.4) * 100) / 100}"`;
}

/** Converts Open-Meteo °C to °F, then formats. */
export function formatTemperatureF(celsius) {
  const numeric = Number(celsius);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round((numeric * 9 / 5 + 32) * 10) / 10} \u00B0F`;
}

/** Converts Open-Meteo km/h to mph, then formats. */
export function formatWindSpeedMph(kilometersPerHour) {
  const numeric = Number(kilometersPerHour);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round((numeric / 1.609344) * 10) / 10} mph`;
}
