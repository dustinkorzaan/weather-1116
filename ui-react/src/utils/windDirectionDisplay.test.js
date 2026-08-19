import { expect, test } from 'vitest';
import { normalizeSourceDegrees } from './windDirectionDisplay';

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
