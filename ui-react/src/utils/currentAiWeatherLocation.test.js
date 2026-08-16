import { expect, test } from 'vitest';
import {
  CURRENT_AI_WEATHER_PATH,
  currentAiWeatherPath,
  locationFromSearchParams,
} from './currentAiWeatherLocation';

test('builds a current AI weather path with a location query', () => {
  expect(currentAiWeatherPath('nashville tn')).toBe(
    '/current-ai-weather?location=nashville%20tn'
  );
  expect(currentAiWeatherPath('New York')).toBe('/current-ai-weather?location=New%20York');
  expect(currentAiWeatherPath('  Atlanta  ')).toBe('/current-ai-weather?location=Atlanta');
  expect(currentAiWeatherPath('')).toBe(CURRENT_AI_WEATHER_PATH);
});

test('reads a location from search params', () => {
  expect(locationFromSearchParams(new URLSearchParams('location=nashville%20tn'))).toBe(
    'nashville tn'
  );
  expect(locationFromSearchParams(new URLSearchParams('location=New+York'))).toBe('New York');
  expect(locationFromSearchParams(new URLSearchParams(''))).toBe('');
});
