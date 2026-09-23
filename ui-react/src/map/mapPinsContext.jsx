import { createContext, useCallback, useContext, useMemo } from 'react';
import {
  useAddUserPinMutation,
  useDeleteUserPinMutation,
  useGetUserQuery,
} from '../services/weatherApi';

const MapPinsContext = createContext(null);

export function MapPinsProvider({ children }) {
  const { data: user, isLoading, error } = useGetUserQuery();
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
