import { normalizeSourceDegrees } from './windDirectionDisplay';

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
  const withDegrees = `(${normalizeSourceDegrees(degrees)}\u00B0)`;
  return label ? `${label} ${withDegrees}` : withDegrees;
}

/** Formats a run-log UTC timestamp, e.g. "14:32:07.123". */
export function formatRunLogTimestamp(dateTimeUtc) {
  const date = new Date(dateTimeUtc);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().slice(11, 23);
}

/** Formats a millisecond duration with thousands separators, e.g. "1,234". */
export function formatRunLogMs(ms) {
  return Number.isFinite(ms) ? Math.round(ms).toLocaleString() : '';
}

/** Formats a run-log token count with thousands separators, e.g. "1,234". */
export function formatRunLogTokenCount(tokens) {
  return Number.isFinite(tokens) ? Math.round(tokens).toLocaleString() : '';
}

export { WIND_DIRECTION_ARROW, normalizeSourceDegrees } from './windDirectionDisplay';
