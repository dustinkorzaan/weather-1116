import { expect, test } from 'vitest';
import {
  formatPrecipitationIn,
  formatTemperatureF,
  formatWindSpeedMph,
} from './weatherGridFormat';

test('formats an already-converted Fahrenheit value with a degree suffix', () => {
  expect(formatTemperatureF(75.2)).toBe('75.2 °F');
  expect(formatTemperatureF(32)).toBe('32 °F');
  expect(formatTemperatureF(Number.NaN)).toBe('');
});

test('formats an already-converted mph value with a lowercase suffix', () => {
  expect(formatWindSpeedMph(6.2)).toBe('6.2 mph');
  expect(formatWindSpeedMph(Number.NaN)).toBe('');
});

test('formats an already-converted inches value rounded to the nearest 1/16"', () => {
  expect(formatPrecipitationIn(1)).toBe('1"');
  expect(formatPrecipitationIn(0)).toBe('0"');
  expect(formatPrecipitationIn(1.5)).toBe('1 1/2"');
  expect(formatPrecipitationIn(2.25)).toBe('2 1/4"');
  expect(formatPrecipitationIn(3.3125)).toBe('3 5/16"');
  expect(formatPrecipitationIn(0.0625)).toBe('1/16"');
  expect(formatPrecipitationIn(Number.NaN)).toBe('');
});
