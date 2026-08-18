import { expect, test } from 'vitest';
import {
  CURRENT_AI_WEATHER_PATH,
  currentAiWeatherPath,
  formatLocationWithLatLong,
  locationFromSearchParams,
} from './currentAiWeatherLocation';

test('encodes a city, state location without cleaning or splitting', () => {
  expect(currentAiWeatherPath('Nashville, TN')).toBe(
    '/current-ai-weather?location=Nashville%2C%20TN'
  );
  expect(currentAiWeatherPath('New York, NY')).toBe(
    '/current-ai-weather?location=New%20York%2C%20NY'
  );
  expect(currentAiWeatherPath('nashville tn')).toBe(
    '/current-ai-weather?location=nashville%20tn'
  );
  expect(currentAiWeatherPath('  Nashville, TN  ')).toBe(
    '/current-ai-weather?location=Nashville%2C%20TN'
  );
  expect(currentAiWeatherPath('')).toBe(CURRENT_AI_WEATHER_PATH);
});

test('expands a city name with hemisphere lat/long', () => {
  expect(formatLocationWithLatLong('Nashville, TN', 36.1659, -86.7844)).toBe(
    'Nashville, TN (36.1659° N, 86.7844° W)'
  );
  expect(formatLocationWithLatLong('Sydney, NSW', -33.8688, 151.2093)).toBe(
    'Sydney, NSW (33.8688° S, 151.2093° E)'
  );
  expect(formatLocationWithLatLong('  Nashville, TN  ', 36.1659, -86.7844)).toBe(
    'Nashville, TN (36.1659° N, 86.7844° W)'
  );
  expect(formatLocationWithLatLong('', 36.1659, -86.7844)).toBe('');
  expect(formatLocationWithLatLong('Nashville, TN', Number.NaN, -86.7844)).toBe('Nashville, TN');
});

test('encodes an expanded pin location into the AI weather query', () => {
  expect(
    currentAiWeatherPath(formatLocationWithLatLong('Nashville, TN', 36.1659, -86.7844))
  ).toBe(
    '/current-ai-weather?location=Nashville%2C%20TN%20(36.1659%C2%B0%20N%2C%2086.7844%C2%B0%20W)'
  );
});

test('reads a location query without reformatting it', () => {
  expect(locationFromSearchParams(new URLSearchParams('location=Nashville%2C%20TN'))).toBe(
    'Nashville, TN'
  );
  expect(locationFromSearchParams(new URLSearchParams('location=nashville%20tn'))).toBe(
    'nashville tn'
  );
  expect(locationFromSearchParams(new URLSearchParams(''))).toBe('');
});
