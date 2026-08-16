import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { cityFromReverseLookup, MAP_DEFAULT_CENTER, MAP_DEFAULT_ZOOM } from '../data/mapCities';
import { applyMapColorSchemeCss, createMapOptions } from '../map/darkMapStyles';
import { loadGoogleMaps } from '../map/loadGoogleMaps';
import {
  createLogoPinOverlay,
  logoPinSpinOffsetSec,
  logoPinUrl,
} from '../map/logoPinOverlay';
import { useMapPins } from '../map/mapPinsContext';
import { bindPinHoverCard } from '../map/pinHoverCard';
import { bindRightClickAddLocation } from '../map/rightClickAddLocation';
import { useLazyGetLocationQuery } from '../services/weatherApi';
import { THEME_CHANGE_EVENT, resolveTheme } from '../theme/theme';
import { currentAiWeatherPath, formatLocationWithLatLong } from '../utils/currentAiWeatherLocation';

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '';

function WeatherMap() {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const mapsApiRef = useRef(null);
  const markersRef = useRef([]);
  const viewStateRef = useRef({
    center: MAP_DEFAULT_CENTER,
    zoom: MAP_DEFAULT_ZOOM,
  });
  const citiesRef = useRef([]);
  const removeCityRef = useRef(() => {});
  const addCityRef = useRef(() => {});
  const getLocationRef = useRef(null);
  const unbindAddLocationRef = useRef(() => {});
  const navigate = useNavigate();
  const { cities, addCity, removeCity } = useMapPins();
  const [triggerGetLocation] = useLazyGetLocationQuery();
  const [status, setStatus] = useState(apiKey ? 'loading' : 'missing-key');
  const [resolvedTheme, setResolvedTheme] = useState(() => resolveTheme());

  citiesRef.current = cities;
  removeCityRef.current = removeCity;
  addCityRef.current = addCity;
  getLocationRef.current = triggerGetLocation;

  useEffect(() => {
    const syncMapTheme = (event) => {
      setResolvedTheme(event?.detail?.resolved ?? resolveTheme());
    };

    window.addEventListener(THEME_CHANGE_EVENT, syncMapTheme);
    return () => window.removeEventListener(THEME_CHANGE_EVENT, syncMapTheme);
  }, []);

  useEffect(() => {
    if (!apiKey || !mapRef.current) {
      return undefined;
    }

    let cancelled = false;
    applyMapColorSchemeCss(mapRef.current, resolvedTheme);

    loadGoogleMaps(apiKey)
      .then((maps) => {
        if (cancelled || !mapRef.current) {
          return;
        }

        applyMapColorSchemeCss(mapRef.current, resolvedTheme);
        const map = new maps.Map(
          mapRef.current,
          createMapOptions(maps, resolvedTheme, {
            center: viewStateRef.current.center,
            zoom: viewStateRef.current.zoom,
          })
        );

        mapsApiRef.current = maps;
        mapInstanceRef.current = map;
        paintMarkers(maps, map, resolvedTheme, navigate, citiesRef, markersRef, removeCityRef);
        unbindAddLocationRef.current = bindRightClickAddLocation({
          maps,
          map,
          onAddLocation: async (lat, lng, controls) => {
            controls.setError('');
            controls.setBusy(true);
            try {
              const lookup = getLocationRef.current;
              if (typeof lookup !== 'function') {
                controls.setError('Unable to find that location.');
                return;
              }
              const data = await lookup({ latitude: lat, longitude: lng }).unwrap();
              const city = cityFromReverseLookup(lat, lng, data);
              if (!city) {
                controls.setError('Unable to find that location.');
                return;
              }
              addCityRef.current(city);
              controls.hide();
            } catch {
              controls.setError('Unable to find that location.');
            } finally {
              controls.setBusy(false);
            }
          },
        });
        if (cancelled) {
          unbindAddLocationRef.current();
          markersRef.current.forEach((pin) => pin.setMap(null));
          return;
        }
        setStatus('ready');
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('error');
        }
      });

    return () => {
      cancelled = true;
      const map = mapInstanceRef.current;
      if (map && typeof map.getCenter === 'function') {
        const center = map.getCenter();
        const zoom = typeof map.getZoom === 'function' ? map.getZoom() : null;
        if (center) {
          viewStateRef.current = {
            center: { lat: center.lat(), lng: center.lng() },
            zoom: zoom ?? viewStateRef.current.zoom,
          };
        }
      }
      unbindAddLocationRef.current();
      unbindAddLocationRef.current = () => {};
      markersRef.current.forEach((pin) => pin.setMap(null));
      markersRef.current = [];
      mapInstanceRef.current = null;
      mapsApiRef.current = null;
    };
  }, [navigate, resolvedTheme]);

  useEffect(() => {
    const maps = mapsApiRef.current;
    const map = mapInstanceRef.current;
    if (!maps || !map) {
      return;
    }

    const previousIds = new Set(markersRef.current.map((pin) => pin.cityId));
    paintMarkers(maps, map, resolvedTheme, navigate, citiesRef, markersRef, removeCityRef);

    const added = citiesRef.current.filter((city) => !previousIds.has(city.id));
    if (added.length === 1 && typeof map.panTo === 'function') {
      map.panTo({ lat: added[0].lat, lng: added[0].lng });
    }
  }, [cities, navigate]);

  return (
    <section className="flex min-h-0 w-full flex-1 flex-col" aria-label="Map">
      {!apiKey && (
        <p className="px-4 py-2 text-destructive">
          Set <code>VITE_GOOGLE_MAPS_API_KEY</code> to enable Google Maps.
        </p>
      )}
      {status === 'error' && (
        <p className="px-4 py-2 text-destructive">
          Unable to load Google Maps. Check the API key and that Maps JavaScript API is enabled.
        </p>
      )}
      <div className="relative min-h-0 w-full flex-1">
        <div
          ref={mapRef}
          className="weather-map h-full w-full bg-[var(--map-canvas)]"
          role="presentation"
          data-status={status}
        />
        {!apiKey && (
          <div className="pointer-events-none absolute top-8 left-8 rounded-lg bg-background p-3 shadow-lg ring-1 ring-foreground/10">
            <div
              className="weather-map-pin-card flex min-w-[10.5rem] flex-col gap-2"
              role="dialog"
              aria-label="Atlanta, GA"
            >
              <div className="weather-map-pin-card-header flex items-start justify-between gap-2">
                <div className="weather-map-pin-card-name text-sm font-semibold text-foreground">
                  Atlanta, GA
                </div>
                <span
                  className="weather-map-pin-card-delete inline-flex size-6 shrink-0 items-center justify-center rounded-md text-muted-foreground"
                  aria-hidden="true"
                >
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <path d="M3 6h18" />
                    <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6" />
                    <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2" />
                    <line x1="10" x2="10" y1="11" y2="17" />
                    <line x1="14" x2="14" y1="11" y2="17" />
                  </svg>
                </span>
              </div>
              <button
                type="button"
                className="weather-map-pin-card-button cursor-pointer rounded-md bg-primary px-2.5 py-1.5 text-left text-sm font-medium text-primary-foreground shadow-sm"
              >
                Get Current AI Weather
              </button>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function paintMarkers(maps, map, resolvedTheme, navigate, citiesRef, markersRef, removeCityRef) {
  markersRef.current.forEach((pin) => pin.setMap(null));

  const logoUrl = logoPinUrl(resolvedTheme);
  const markers = citiesRef.current.map((city, index) => {
    const overlay = createLogoPinOverlay(maps, {
      lat: city.lat,
      lng: city.lng,
      cityName: city.name,
      logoUrl,
      spinOffsetSec: logoPinSpinOffsetSec(index),
    });
    overlay.cityId = city.id;
    overlay.setMap(map);

    bindPinHoverCard({
      maps,
      map,
      marker: overlay,
      cityName: city.name,
      onGetWeather: () => {
        navigate(currentAiWeatherPath(formatLocationWithLatLong(city.name, city.lat, city.lng)));
      },
      onDelete: () => {
        removeCityRef.current(city.id);
      },
    });

    return overlay;
  });

  markersRef.current = markers;
}

export default WeatherMap;
