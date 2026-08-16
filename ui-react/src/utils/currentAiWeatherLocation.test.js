import { expect, test } from 'vitest';
import {
  CURRENT_AI_WEATHER_PATH,
  currentAiWeatherPath,
  locationFromSearchParams,
} from './currentAiWeatherLocation';

test('encodes a city, state location without cleaning or splitting', () => {
  expect(currentAiWeatherPath('Atlanta, GA')).toBe(
    '/current-ai-weather?location=Atlanta%2C%20GA'
  );
  expect(currentAiWeatherPath('New York, NY')).toBe(
    '/current-ai-weather?location=New%20York%2C%20NY'
  );
  expect(currentAiWeatherPath('nashville tn')).toBe(
    '/current-ai-weather?location=nashville%20tn'
  );
  expect(currentAiWeatherPath('  Atlanta, GA  ')).toBe(
    '/current-ai-weather?location=Atlanta%2C%20GA'
  );
  expect(currentAiWeatherPath('')).toBe(CURRENT_AI_WEATHER_PATH);
});

test('reads a location query without reformatting it', () => {
  expect(locationFromSearchParams(new URLSearchParams('location=Atlanta%2C%20GA'))).toBe(
    'Atlanta, GA'
  );
  expect(locationFromSearchParams(new URLSearchParams('location=nashville%20tn'))).toBe(
    'nashville tn'
  );
  expect(locationFromSearchParams(new URLSearchParams(''))).toBe('');
});
