/** Sample city pins for the weather map. Temperature reserved for a later pass. */
export const MAP_CITIES = [
  { id: '59e2459a-b25d-44a7-bcb0-2a4f2e444272', name: 'New York, NY', lat: 40.7128, lng: -74.006 },
  { id: '329735f1-cfc0-42b4-a48f-0d41677145e8', name: 'Toronto, ON', lat: 43.6532, lng: -79.3832 },
  { id: '9daab691-7885-400f-8aed-5e21a63f9a7a', name: 'Atlanta, GA', lat: 33.749, lng: -84.388 },
  { id: '04f5d22f-ca31-4d29-ac9e-a1c4f0127ed1', name: 'Charlotte, NC', lat: 35.2271, lng: -80.8431 },
];

/** Default map center (Eastern US / SE Canada). */
export const MAP_DEFAULT_CENTER = { lat: 39.5, lng: -77.5 };
export const MAP_DEFAULT_ZOOM = 5;

export const MAP_CITIES_STORAGE_KEY = 'weather-map-cities';

function isValidCity(city) {
  return (
    city &&
    typeof city.id === 'string' &&
    city.id &&
    typeof city.name === 'string' &&
    city.name &&
    Number.isFinite(city.lat) &&
    Number.isFinite(city.lng)
  );
}

/** Loads the mutable pin list, seeding the sample cities on first visit. */
export function loadMapCities() {
  try {
    const raw = window.sessionStorage?.getItem(MAP_CITIES_STORAGE_KEY);
    if (raw != null) {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed) && parsed.every(isValidCity)) {
        return parsed;
      }
    }
  } catch {
    // Ignore quota / private-mode / malformed JSON and fall back.
  }

  return MAP_CITIES.map((city) => ({ ...city }));
}

export function saveMapCities(cities) {
  try {
    window.sessionStorage?.setItem(MAP_CITIES_STORAGE_KEY, JSON.stringify(cities));
  } catch {
    // Ignore quota / private-mode failures.
  }
}

export function newCityId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (char) => {
    const nibble = (Math.random() * 16) | 0;
    const value = char === 'x' ? nibble : (nibble & 0x3) | 0x8;
    return value.toString(16);
  });
}

/**
 * Builds a map pin from the first GET /Geo match.
 * @returns {{ id: string, name: string, lat: number, lng: number } | null}
 */
export function cityFromLatLongSearch(locationInput, data) {
  const lat = Number(data?.latitude);
  const lng = Number(data?.longitude);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
    return null;
  }

  const resolved = [data?.name, data?.state]
    .map((part) => String(part || '').trim())
    .filter(Boolean)
    .join(', ');
  const name = resolved || String(locationInput || '').trim();
  if (!name) {
    return null;
  }

  return {
    id: newCityId(),
    name,
    lat,
    lng,
  };
}

export function upsertMapCity(cities, city) {
  if (!isValidCity(city)) {
    return cities;
  }

  const existingIndex = cities.findIndex(
    (item) => item.id === city.id || (item.lat === city.lat && item.lng === city.lng)
  );
  if (existingIndex >= 0) {
    const next = cities.slice();
    next[existingIndex] = { ...cities[existingIndex], ...city };
    return next;
  }

  return [...cities, city];
}

export function removeMapCity(cities, cityId) {
  return cities.filter((city) => city.id !== cityId);
}
