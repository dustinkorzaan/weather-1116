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

/** Formats an Open-Meteo hourly/15-minute timestamp ("2026-08-19T14:00") as "Aug 19, 2:00 PM". */
export function formatClockTime(isoDateTime) {
  const date = new Date(isoDateTime);
  if (Number.isNaN(date.getTime())) {
    return isoDateTime ?? '';
  }

  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export function formatPrecipitationIn(value) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 100) / 100} in`;
}
