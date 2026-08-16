import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  MAP_CITIES,
  MAP_DEFAULT_CENTER,
  MAP_DEFAULT_ZOOM,
} from '../data/mapCities';
import { applyMapColorSchemeCss, createMapOptions } from '../map/darkMapStyles';
import { loadGoogleMaps } from '../map/loadGoogleMaps';
import {
  createLogoPinOverlay,
  logoPinSpinOffsetSec,
  logoPinUrl,
} from '../map/logoPinOverlay';
import { bindPinHoverCard } from '../map/pinHoverCard';
import { THEME_CHANGE_EVENT, resolveTheme } from '../theme/theme';
import { currentAiWeatherPath } from '../utils/currentAiWeatherLocation';

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '';

function WeatherMap() {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const markersRef = useRef([]);
  const viewStateRef = useRef({
    center: MAP_DEFAULT_CENTER,
    zoom: MAP_DEFAULT_ZOOM,
  });
  const navigate = useNavigate();
  const [status, setStatus] = useState(apiKey ? 'loading' : 'missing-key');
  const [resolvedTheme, setResolvedTheme] = useState(() => resolveTheme());

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
    let pins = [];
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

        const logoUrl = logoPinUrl(resolvedTheme);
        const markers = MAP_CITIES.map((city, index) => {
          const overlay = createLogoPinOverlay(maps, {
            lat: city.lat,
            lng: city.lng,
            cityName: city.name,
            logoUrl,
            spinOffsetSec: logoPinSpinOffsetSec(index),
          });
          overlay.setMap(map);

          bindPinHoverCard({
            maps,
            map,
            marker: overlay,
            cityName: city.name,
            onGetWeather: (cityName) => {
              navigate(currentAiWeatherPath(cityName));
            },
            onHoverChange: (hovered) => overlay.setPaused(hovered),
          });

          return overlay;
        });

        pins = markers;
        mapInstanceRef.current = map;
        markersRef.current = markers;
        if (cancelled) {
          pins.forEach((pin) => pin.setMap(null));
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
      pins.forEach((pin) => pin.setMap(null));
      mapInstanceRef.current = null;
      markersRef.current = [];
    };
  }, [navigate, resolvedTheme]);

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
              <div className="weather-map-pin-card-name text-sm font-semibold text-foreground">
                Atlanta, GA
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

export default WeatherMap;
