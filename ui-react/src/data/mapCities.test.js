import { expect, test } from 'vitest';
import { MAP_CITIES } from './mapCities';
import { currentAiWeatherPath, locationSearchValue } from '../utils/currentAiWeatherLocation';

test('map pins use city and state labels', () => {
  expect(MAP_CITIES.map((city) => city.name)).toEqual([
    'New York, NY',
    'Toronto, ON',
    'Atlanta, GA',
    'Charlotte, NC',
  ]);
});

test('map pin labels clean to a query and expand in the search box', () => {
  expect(currentAiWeatherPath('Atlanta, GA')).toBe('/current-ai-weather?location=Atlanta%20GA');
  expect(locationSearchValue('Atlanta GA')).toBe('Atlanta, GA');
});
