import { expect, test } from 'vitest';
import {
  formatPrecipitationIn,
  formatTemperatureF,
  formatWindSpeedMph,
} from './weatherGridFormat';

test('converts Open-Meteo Celsius to a degree Fahrenheit suffix', () => {
  expect(formatTemperatureF(24)).toBe('75.2 °F');
  expect(formatTemperatureF(0)).toBe('32 °F');
  expect(formatTemperatureF(Number.NaN)).toBe('');
});

test('converts Open-Meteo km/h to a lowercase mph suffix', () => {
  expect(formatWindSpeedMph(10)).toBe('6.2 mph');
  expect(formatWindSpeedMph(Number.NaN)).toBe('');
});

test('converts Open-Meteo millimeters to inches', () => {
  expect(formatPrecipitationIn(25.4)).toBe('1"');
  expect(formatPrecipitationIn(7.62)).toBe('0.3"');
  expect(formatPrecipitationIn(Number.NaN)).toBe('');
});
