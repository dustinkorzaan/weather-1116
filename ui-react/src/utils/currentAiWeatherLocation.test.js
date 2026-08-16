import { expect, test } from 'vitest';
import {
  CURRENT_AI_WEATHER_PATH,
  cleanLocationQuery,
  currentAiWeatherPath,
  locationFromSearchParams,
  locationSearchValue,
} from './currentAiWeatherLocation';

test('cleans and splits a city, state location for the query string', () => {
  expect(cleanLocationQuery('Atlanta, GA')).toBe('Atlanta GA');
  expect(cleanLocationQuery('Atlanta,GA')).toBe('Atlanta GA');
  expect(cleanLocationQuery('  New York,  NY  ')).toBe('New York NY');
  expect(cleanLocationQuery('nashville tn')).toBe('nashville tn');
  expect(cleanLocationQuery('')).toBe('');
});

test('formats a cleaned location for the search box', () => {
  expect(locationSearchValue('Atlanta, GA')).toBe('Atlanta, GA');
  expect(locationSearchValue('Atlanta GA')).toBe('Atlanta, GA');
  expect(locationSearchValue('New York NY')).toBe('New York, NY');
  expect(locationSearchValue('nashville tn')).toBe('nashville, TN');
  expect(locationSearchValue('')).toBe('');
});

test('builds a current AI weather path with a cleaned location query', () => {
  expect(currentAiWeatherPath('Atlanta, GA')).toBe('/current-ai-weather?location=Atlanta%20GA');
  expect(currentAiWeatherPath('nashville tn')).toBe(
    '/current-ai-weather?location=nashville%20tn'
  );
  expect(currentAiWeatherPath('  Atlanta  ')).toBe('/current-ai-weather?location=Atlanta');
  expect(currentAiWeatherPath('')).toBe(CURRENT_AI_WEATHER_PATH);
});

test('reads a location query and expands it for the search box', () => {
  expect(locationFromSearchParams(new URLSearchParams('location=Atlanta%20GA'))).toBe(
    'Atlanta, GA'
  );
  expect(locationFromSearchParams(new URLSearchParams('location=nashville%20tn'))).toBe(
    'nashville, TN'
  );
  expect(locationFromSearchParams(new URLSearchParams('location=New+York+NY'))).toBe(
    'New York, NY'
  );
  expect(locationFromSearchParams(new URLSearchParams(''))).toBe('');
});
