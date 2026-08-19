import { expect, test } from 'vitest';
import { WIND_DIRECTION_ARROW, normalizeSourceDegrees } from './windDirectionDisplay';

test('uses a letter v as the down-pointing wind direction marker', () => {
  expect(WIND_DIRECTION_ARROW).toBe('v');
});

test('normalizes source degrees to 0–360 and treats non-finite as 0', () => {
  expect(normalizeSourceDegrees(224)).toBe(224);
  expect(normalizeSourceDegrees(180)).toBe(180);
  expect(normalizeSourceDegrees(540)).toBe(180);
  expect(normalizeSourceDegrees(-90)).toBe(270);
  expect(normalizeSourceDegrees(360)).toBe(0);
  expect(normalizeSourceDegrees(Number.NaN)).toBe(0);
  expect(normalizeSourceDegrees(Number.POSITIVE_INFINITY)).toBe(0);
  expect(normalizeSourceDegrees(Number.NEGATIVE_INFINITY)).toBe(0);
});
