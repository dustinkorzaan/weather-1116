/** Down-pointing arrow (⮛); at 0° rotation the glyph points south (wind from north). */
export const WIND_DIRECTION_ARROW = '\u2B9B';

/** CSS rotate degrees for ⮛ from meteorological source degrees; null when not finite. */
export function windArrowRotationDeg(sourceDegrees) {
  const numeric = Number(sourceDegrees);
  return Number.isFinite(numeric) ? numeric : null;
}
