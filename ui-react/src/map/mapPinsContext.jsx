import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import {
  loadMapCities,
  removeMapCity,
  saveMapCities,
  upsertMapCity,
} from '../data/mapCities';

const MapPinsContext = createContext(null);

export function MapPinsProvider({ children }) {
  const [cities, setCities] = useState(() => loadMapCities());

  const addCity = useCallback((city) => {
    setCities((prev) => {
      const next = upsertMapCity(prev, city);
      saveMapCities(next);
      return next;
    });
  }, []);

  const removeCity = useCallback((cityId) => {
    setCities((prev) => {
      const next = removeMapCity(prev, cityId);
      saveMapCities(next);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ cities, addCity, removeCity }),
    [cities, addCity, removeCity]
  );

  return <MapPinsContext.Provider value={value}>{children}</MapPinsContext.Provider>;
}

export function useMapPins() {
  const value = useContext(MapPinsContext);
  if (!value) {
    throw new Error('useMapPins must be used within MapPinsProvider.');
  }

  return value;
}
