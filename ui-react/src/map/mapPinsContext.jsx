import { createContext, useCallback, useContext, useEffect, useMemo, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import {
  useAddUserCityMutation,
  useDeleteUserCityMutation,
  useGetUserQuery,
} from '../services/weatherApi';

const MapPinsContext = createContext(null);

export function MapPinsProvider({ children }) {
  const { data: user, isLoading, error, refetch } = useGetUserQuery();
  const { pathname, key: locationKey } = useLocation();
  const hasMountedRef = useRef(false);

  // Pins can change outside this tab's mutations (a chat agent calling AddUserCity,
  // or another browser tab), so every navigation to Home re-reads the user.
  // locationKey changes even when Home is clicked while already on Home.
  useEffect(() => {
    if (!hasMountedRef.current) {
      hasMountedRef.current = true;
      return;
    }

    if (pathname === '/') {
      refetch();
    }
  }, [pathname, locationKey, refetch]);
  const [addUserCity] = useAddUserCityMutation();
  const [deleteUserCity] = useDeleteUserCityMutation();

  const cities = user?.userCities?.map((userCity) => ({
    id: userCity.id,
    locationName: userCity.locationName,
    latitude: userCity.latitude,
    longitude: userCity.longitude,
  })) ?? [];

  const addCity = useCallback(
    async (city) => {
      try {
        await addUserCity({
          latitude: city.latitude,
          longitude: city.longitude,
          locationName: city.locationName,
        }).unwrap();
      } catch (err) {
        console.error('Failed to add city:', err);
      }
    },
    [addUserCity]
  );

  const removeCity = useCallback(
    async (cityId) => {
      try {
        await deleteUserCity(cityId).unwrap();
      } catch (err) {
        console.error('Failed to remove city:', err);
      }
    },
    [deleteUserCity]
  );

  const value = useMemo(
    () => ({ cities, addCity, removeCity, isLoading, error }),
    [cities, addCity, removeCity, isLoading, error]
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
