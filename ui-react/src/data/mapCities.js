/** Default map center (Nashville, TN / south-central US). */
export const MAP_DEFAULT_CENTER = { lat: 36.16, lng: -86.78 };
export const MAP_DEFAULT_ZOOM = 4;

function isValidCity(city) {
  return (
    city &&
    typeof city.id === 'string' &&
    city.id &&
    typeof city.locationName === 'string' &&
    city.locationName &&
    Number.isFinite(city.latitude) &&
    Number.isFinite(city.longitude)
  );
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
 * Builds a map pin from GET /Geo/GetLocation, keeping the clicked coordinates.
 * @returns {{ id: string, locationName: string, latitude: number, longitude: number } | null}
 */
export function cityFromReverseLookup(lat, lng, data) {
  const locationName = String(data?.location || '').trim();
  if (!locationName || !Number.isFinite(Number(lat)) || !Number.isFinite(Number(lng))) {
    return null;
  }

  return {
    id: newCityId(),
    locationName,
    latitude: Number(lat),
    longitude: Number(lng),
  };
}

/**
 * Builds a map pin from the first GET /Geo match.
 * @returns {{ id: string, locationName: string, latitude: number, longitude: number } | null}
 */
export function cityFromLatLongSearch(locationInput, data) {
  const latitude = Number(data?.latitude);
  const longitude = Number(data?.longitude);
  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return null;
  }

  const resolved = [data?.name, data?.state]
    .map((part) => String(part || '').trim())
    .filter(Boolean)
    .join(', ');
  const locationName = resolved || String(locationInput || '').trim();
  if (!locationName) {
    return null;
  }

  return {
    id: newCityId(),
    locationName,
    latitude,
    longitude,
  };
}

export function upsertMapCity(cities, city) {
  if (!isValidCity(city)) {
    return cities;
  }

  const existingIndex = cities.findIndex(
    (item) => item.id === city.id || (item.latitude === city.latitude && item.longitude === city.longitude)
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
