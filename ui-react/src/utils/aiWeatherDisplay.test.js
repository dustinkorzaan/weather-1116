import { expect, test } from 'vitest';
import {
  formatLatLong,
  formatRunLogMs,
  formatRunLogTimestamp,
  formatRunLogTokenCount,
  formatTemperatureF,
  formatWindDirection,
  formatWindSpeedMph,
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
  expect(formatWindDirection('SW', Number.NaN)).toBe('SW (0°)');
  expect(formatWindDirection('SW', 540)).toBe('SW (180°)');
  expect(formatWindDirection('', 224)).toBe('(224°)');
});

test('formats a run-log timestamp as UTC time-of-day with milliseconds', () => {
  expect(formatRunLogTimestamp('2026-08-19T14:32:07.123Z')).toBe('14:32:07.123');
  expect(formatRunLogTimestamp('not-a-date')).toBe('');
});

test('formats a run-log millisecond duration with thousands separators', () => {
  expect(formatRunLogMs(0)).toBe('0');
  expect(formatRunLogMs(1234)).toBe('1,234');
  expect(formatRunLogMs(1234.6)).toBe('1,235');
  expect(formatRunLogMs(Number.NaN)).toBe('');
});

test('formats a run-log token count with thousands separators', () => {
  expect(formatRunLogTokenCount(0)).toBe('0');
  expect(formatRunLogTokenCount(1234)).toBe('1,234');
  expect(formatRunLogTokenCount(null)).toBe('');
  expect(formatRunLogTokenCount(undefined)).toBe('');
});
