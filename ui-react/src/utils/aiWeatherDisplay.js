function formatHemisphereDegrees(value, positiveLabel, negativeLabel) {
  const numeric = Number(value);
  const hemisphere = numeric >= 0 ? positiveLabel : negativeLabel;
  return `${Math.abs(numeric).toFixed(2)}\u00B0 ${hemisphere}`;
}

/** Formats coordinates as "36.16° N, 86.78° W". */
export function formatLatLong(lat, lng) {
  if (!Number.isFinite(Number(lat)) || !Number.isFinite(Number(lng))) {
    return '';
  }

  return `${formatHemisphereDegrees(lat, 'N', 'S')}, ${formatHemisphereDegrees(lng, 'E', 'W')}`;
}

export function formatTemperatureF(value) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 10) / 10} \u00B0F`;
}

export function formatWindSpeedMph(value) {
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  return `${Math.round(numeric * 10) / 10} mph`;
}

/** Formats compass plus source degrees as "SW (224°)". */
export function formatWindDirection(compass, degrees) {
  const label = String(compass ?? '').trim();
  const numeric = Number(degrees);
  if (!Number.isFinite(numeric)) {
    return label;
  }

  const withDegrees = `(${Math.round(numeric)}\u00B0)`;
  return label ? `${label} ${withDegrees}` : withDegrees;
}

/** Down-pointing arrow (⮛); at 0° rotation the glyph points south (wind from north). */
export const WIND_DIRECTION_ARROW = '\u2B9B';
