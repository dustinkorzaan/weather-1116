const COMPASS_POINTS = [
  'N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE',
  'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW',
];

/** Down-pointing arrow (⮛); at 0° rotation the glyph points south (wind from north). */
export const WIND_DIRECTION_ARROW = '\u2B9B';

/** CSS rotate degrees for ⮛ from meteorological source degrees; null when not finite. */
export function windArrowRotationDeg(sourceDegrees) {
  const numeric = Number(sourceDegrees);
  return Number.isFinite(numeric) ? numeric : null;
}

/** Expects source degrees already normalized to 0–360 by the API mapper. */
export function degreesToCompass(degrees) {
  const numeric = Number(degrees);
  if (!Number.isFinite(numeric)) {
    return '';
  }

  const index = Math.round(numeric / 22.5) % 16;
  return COMPASS_POINTS[index];
}
