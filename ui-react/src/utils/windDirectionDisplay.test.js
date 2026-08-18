import { expect, test } from 'vitest';
import {
  degreesToCompass,
  WIND_DIRECTION_ARROW,
  windArrowRotationDeg,
} from './windDirectionDisplay';

test('uses the down-pointing wind direction arrow glyph', () => {
  expect(WIND_DIRECTION_ARROW).toBe('\u2B9B');
});

test('wind arrow rotation uses source degrees directly', () => {
  expect(windArrowRotationDeg(0)).toBe(0);
  expect(windArrowRotationDeg(180)).toBe(180);
  expect(windArrowRotationDeg(224)).toBe(224);
  expect(windArrowRotationDeg(Number.NaN)).toBeNull();
});

test('degreesToCompass maps normalized source degrees to sixteen points', () => {
  expect(degreesToCompass(0)).toBe('N');
  expect(degreesToCompass(90)).toBe('E');
  expect(degreesToCompass(180)).toBe('S');
  expect(degreesToCompass(224)).toBe('SW');
  expect(degreesToCompass(Number.NaN)).toBe('');
});
