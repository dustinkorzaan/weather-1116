import { createContext, useCallback, useContext, useEffect, useMemo, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import {
  useAddUserPinMutation,
  useDeleteUserPinMutation,
  useGetUserQuery,
} from '../services/weatherApi';

const MapPinsContext = createContext(null);

export function MapPinsProvider({ children }) {
  const { data: user, isLoading, error, refetch } = useGetUserQuery();
  const { pathname, key: locationKey } = useLocation();
  const hasMountedRef = useRef(false);

  // Pins can change outside this tab's mutations (a chat agent calling AddUserPin,
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
  const [addUserPin] = useAddUserPinMutation();
  const [deleteUserPin] = useDeleteUserPinMutation();

  const cities = user?.userPins?.map((pin) => ({
    id: pin.id,
    locationName: pin.locationName,
    latitude: pin.latitude,
    longitude: pin.longitude,
  })) ?? [];

  const addCity = useCallback(
    async (city) => {
      try {
        await addUserPin({
          latitude: city.latitude,
          longitude: city.longitude,
          locationName: city.locationName,
        }).unwrap();
      } catch (err) {
        console.error('Failed to add city:', err);
      }
    },
    [addUserPin]
  );

  const removeCity = useCallback(
    async (cityId) => {
      try {
        await deleteUserPin(cityId).unwrap();
      } catch (err) {
        console.error('Failed to remove city:', err);
      }
    },
    [deleteUserPin]
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
