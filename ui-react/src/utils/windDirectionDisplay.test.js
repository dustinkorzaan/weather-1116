import { expect, test } from 'vitest';
import {
  WIND_DIRECTION_ARROW,
  windArrowRotationDeg,
} from './windDirectionDisplay';

test('uses the down-pointing wind direction arrow glyph', () => {
  expect(WIND_DIRECTION_ARROW).toBe('\u2B9B');
});

test('wind arrow rotation uses source degrees directly', () => {
  expect(windArrowRotationDeg(180)).toBe(180);
  expect(windArrowRotationDeg(224)).toBe(224);
  expect(windArrowRotationDeg(Number.NaN)).toBeNull();
});
