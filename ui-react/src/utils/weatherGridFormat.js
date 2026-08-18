export { degreesToCompass } from './windDirectionDisplay';

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

/** Reduces a sixteenths-of-an-inch numerator to lowest terms (denominator is always a power of two). */
function reduceSixteenths(numerator) {
  let denominator = 16;
  while (numerator !== 0 && numerator % 2 === 0 && denominator > 1) {
    numerator /= 2;
    denominator /= 2;
  }
  return [numerator, denominator];
}

/** Formats an already-converted inches value (the API returns US customary units) rounded to the nearest 1/16", e.g. "1 1/2"". Negative values (an upstream data artifact) are treated as zero. */
export function formatPrecipitationIn(inches) {
  const numeric = Number(inches);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  const sixteenths = Math.round(Math.max(0, numeric) * 16);
  const whole = Math.floor(sixteenths / 16);
  const remainder = sixteenths % 16;

  if (remainder === 0) {
    return `${whole}"`;
  }

  const [num, den] = reduceSixteenths(remainder);
  return whole === 0 ? `${num}/${den}"` : `${whole} ${num}/${den}"`;
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
