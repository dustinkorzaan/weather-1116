/** Down-pointing arrow (U+2B9B); at 0° rotation the glyph points south (wind from north). */
export const WIND_DIRECTION_ARROW = '\u2B9B';

/** Wrap meteorological source degrees to 0–360. NaN / Infinity become 0. */
export function normalizeSourceDegrees(deg) {
  const numeric = Number(deg);
  if (!Number.isFinite(numeric)) {
    return 0;
  }
  return Math.round(((numeric % 360) + 360) % 360);
}
