import { expect, test } from 'vitest';
import { MAP_CITIES } from './mapCities';
import { currentAiWeatherPath } from '../utils/currentAiWeatherLocation';

test('map pins use city and state labels', () => {
  expect(MAP_CITIES.map((city) => city.name)).toEqual([
    'New York, NY',
    'Toronto, ON',
    'Atlanta, GA',
    'Charlotte, NC',
  ]);
});

test('map pin labels encode into the location query', () => {
  expect(currentAiWeatherPath('Atlanta, GA')).toBe(
    '/current-ai-weather?location=Atlanta%2C%20GA'
  );
});
