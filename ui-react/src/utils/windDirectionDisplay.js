/** Letter v points down; at 0° rotation it points south (wind from north). */
export const WIND_DIRECTION_ARROW = 'v';

/** Wrap meteorological source degrees to 0–360. NaN / Infinity become 0. */
export function normalizeSourceDegrees(deg) {
  const numeric = Number(deg);
  if (!Number.isFinite(numeric)) {
    return 0;
  }
  return Math.round(((numeric % 360) + 360) % 360);
}
