import { expect, test } from 'vitest';
import {
  formatPrecipitationMm,
  formatTemperatureC,
  formatWindSpeedKmh,
} from './weatherGridFormat';

test('formats Open-Meteo temperature with a degree Celsius suffix', () => {
  expect(formatTemperatureC(88.44)).toBe('88.4 °C');
  expect(formatTemperatureC(24)).toBe('24 °C');
  expect(formatTemperatureC(Number.NaN)).toBe('');
});

test('formats Open-Meteo wind speed with a km/h suffix', () => {
  expect(formatWindSpeedKmh(12.34)).toBe('12.3 km/h');
  expect(formatWindSpeedKmh(7.5)).toBe('7.5 km/h');
  expect(formatWindSpeedKmh(Number.NaN)).toBe('');
});

test('formats Open-Meteo precipitation with an mm suffix', () => {
  expect(formatPrecipitationMm(0.30000000000000004)).toBe('0.3 mm');
  expect(formatPrecipitationMm(Number.NaN)).toBe('');
});
