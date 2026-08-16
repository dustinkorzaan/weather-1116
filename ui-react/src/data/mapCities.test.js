import { afterEach, expect, test } from 'vitest';
import {
  MAP_CITIES,
  MAP_CITIES_STORAGE_KEY,
  cityFromAiWeather,
  cityIdFromCoordinates,
  loadMapCities,
  removeMapCity,
  saveMapCities,
  upsertMapCity,
} from './mapCities';
import { currentAiWeatherPath } from '../utils/currentAiWeatherLocation';

afterEach(() => {
  window.sessionStorage.removeItem(MAP_CITIES_STORAGE_KEY);
});

test('map pins use city and state labels', () => {
  expect(MAP_CITIES.map((city) => city.name)).toEqual([
    'New York, NY',
    'Toronto, ON',
    'Atlanta, GA',
    'Charlotte, NC',
  ]);
});

test('default map cities use GUID ids', () => {
  const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  expect(MAP_CITIES.map((city) => city.id)).toEqual([
    '59e2459a-b25d-44a7-bcb0-2a4f2e444272',
    '329735f1-cfc0-42b4-a48f-0d41677145e8',
    '9daab691-7885-400f-8aed-5e21a63f9a7a',
    '04f5d22f-ca31-4d29-ac9e-a1c4f0127ed1',
  ]);
  for (const city of MAP_CITIES) {
    expect(city.id).toMatch(guid);
  }
});

test('map pin labels encode into the location query', () => {
  expect(currentAiWeatherPath('Atlanta, GA')).toBe(
    '/current-ai-weather?location=Atlanta%2C%20GA'
  );
});

test('loadMapCities seeds sample cities then round-trips session storage', () => {
  expect(loadMapCities().map((city) => city.id)).toEqual(MAP_CITIES.map((city) => city.id));

  const next = [{ id: 'nashville', name: 'Nashville, TN', lat: 36.16, lng: -86.78 }];
  saveMapCities(next);
  expect(loadMapCities()).toEqual(next);
});

test('cityFromAiWeather requires coordinates and prefers the resolved name', () => {
  expect(cityFromAiWeather('37201', null)).toBeNull();
  expect(
    cityFromAiWeather('37201', {
      locationName: 'Nashville, TN',
      latitude: 36.1627,
      longitude: -86.7816,
    })
  ).toEqual({
    id: cityIdFromCoordinates(36.1627, -86.7816),
    name: 'Nashville, TN',
    lat: 36.1627,
    lng: -86.7816,
  });
});

test('upsertMapCity appends a new pin and removeMapCity drops it', () => {
  const nashville = {
    id: 'nashville',
    name: 'Nashville, TN',
    lat: 36.16,
    lng: -86.78,
  };
  const added = upsertMapCity(MAP_CITIES, nashville);
  expect(added).toHaveLength(MAP_CITIES.length + 1);
  expect(added.at(-1)).toEqual(nashville);
  expect(removeMapCity(added, 'nashville')).toEqual(MAP_CITIES);
});
