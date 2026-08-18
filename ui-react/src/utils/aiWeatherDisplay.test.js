import { expect, test } from 'vitest';
import {
  formatLatLong,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
  WIND_DIRECTION_ARROW,
} from './aiWeatherDisplay';

test('formats lat/long with two-decimal hemisphere labels', () => {
  expect(formatLatLong(36.1627, -86.7816)).toBe('36.16° N, 86.78° W');
  expect(formatLatLong(36.16, -86.78)).toBe('36.16° N, 86.78° W');
  expect(formatLatLong(-33.8688, 151.2093)).toBe('33.87° S, 151.21° E');
  expect(formatLatLong(Number.NaN, -86.78)).toBe('');
  expect(formatLatLong(36.16, Number.NaN)).toBe('');
});

test('formats temperature with a degree Fahrenheit suffix', () => {
  expect(formatTemperatureF(100)).toBe('100 °F');
  expect(formatTemperatureF(72.5)).toBe('72.5 °F');
  expect(formatTemperatureF(Number.NaN)).toBe('');
});

test('formats wind speed with a lowercase mph suffix', () => {
  expect(formatWindSpeedMph(13)).toBe('13 mph');
  expect(formatWindSpeedMph(5.5)).toBe('5.5 mph');
  expect(formatWindSpeedMph(Number.NaN)).toBe('');
});

test('formats wind direction as compass plus degrees', () => {
  expect(formatWindDirection('SW', 224)).toBe('SW (224°)');
  expect(formatWindDirection('S', 180)).toBe('S (180°)');
  expect(formatWindDirection(' N ', 10.4)).toBe('N (10°)');
  expect(formatWindDirection('SW', Number.NaN)).toBe('SW');
  expect(formatWindDirection('', 224)).toBe('(224°)');
});

test('uses the down-pointing wind direction arrow glyph', () => {
  expect(WIND_DIRECTION_ARROW).toBe('\u2B9B');
});
