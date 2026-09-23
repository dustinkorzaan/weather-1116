import { expect, test } from 'vitest';
import {
  MAP_DEFAULT_CENTER,
  MAP_DEFAULT_ZOOM,
  cityFromLatLongSearch,
  cityFromReverseLookup,
  removeMapCity,
  upsertMapCity,
} from './mapCities';
import { currentAiWeatherPath, formatLocationWithLatLong } from '../utils/currentAiWeatherLocation';

test('default map view is Nashville at zoom 4', () => {
  expect(MAP_DEFAULT_CENTER).toEqual({ lat: 36.16, lng: -86.78 });
  expect(MAP_DEFAULT_ZOOM).toBe(4);
});

test('map pin labels encode into the location query with lat/long', () => {
  expect(currentAiWeatherPath(formatLocationWithLatLong('Nashville, TN', 36.1659, -86.7844))).toBe(
    '/current-ai-weather?location=Nashville%2C%20TN%20(36.1659%C2%B0%20N%2C%2086.7844%C2%B0%20W)'
  );
});

test('cityFromLatLongSearch uses the first geo match name and coordinates', () => {
  expect(cityFromLatLongSearch('37201', null)).toBeNull();
  const city = cityFromLatLongSearch('37201', {
    name: 'Nashville',
    state: 'Tennessee',
    latitude: 36.1627,
    longitude: -86.7816,
  });
  expect(city).toMatchObject({
    locationName: 'Nashville, Tennessee',
    latitude: 36.1627,
    longitude: -86.7816,
  });
  expect(city.id).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
  );
});

test('cityFromReverseLookup uses the clicked coordinates and GetLocation label', () => {
  expect(cityFromReverseLookup(36.1627, -86.7816, null)).toBeNull();
  expect(cityFromReverseLookup(36.1627, -86.7816, { location: '  ' })).toBeNull();
  const city = cityFromReverseLookup(36.1627, -86.7816, { location: 'Nashville, Tennessee' });
  expect(city).toMatchObject({
    locationName: 'Nashville, Tennessee',
    latitude: 36.1627,
    longitude: -86.7816,
  });
  expect(city.id).toMatch(
    /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i
  );
});

test('upsertMapCity appends a new pin and removeMapCity drops it', () => {
  const initialCities = [
    { id: 'id1', locationName: 'City 1', latitude: 40.7128, longitude: -74.006 },
    { id: 'id2', locationName: 'City 2', latitude: 43.6532, longitude: -79.3832 },
  ];
  const nashville = {
    id: 'nashville',
    locationName: 'Nashville, TN',
    latitude: 36.16,
    longitude: -86.78,
  };
  const added = upsertMapCity(initialCities, nashville);
  expect(added).toHaveLength(initialCities.length + 1);
  expect(added.at(-1)).toEqual(nashville);
  expect(removeMapCity(added, 'nashville')).toEqual(initialCities);
});
