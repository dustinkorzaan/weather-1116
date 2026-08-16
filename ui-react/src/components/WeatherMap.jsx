import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  MAP_CITIES,
  MAP_DEFAULT_CENTER,
  MAP_DEFAULT_ZOOM,
} from '../data/mapCities';
import {
  applyMapAppearance,
  createPinIcon,
  getMapAppearance,
} from '../map/darkMapStyles';
import { loadGoogleMaps } from '../map/loadGoogleMaps';
import { bindPinHoverCard } from '../map/pinHoverCard';
import { THEME_CHANGE_EVENT, resolveTheme } from '../theme/theme';
import { currentAiWeatherPath } from '../utils/currentAiWeatherLocation';

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '';

function WeatherMap() {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
  const mapsApiRef = useRef(null);
  const markersRef = useRef([]);
  const navigate = useNavigate();
  const [status, setStatus] = useState(apiKey ? 'loading' : 'missing-key');

  useEffect(() => {
    if (!apiKey || !mapRef.current || mapInstanceRef.current) {
      return undefined;
    }

    let cancelled = false;

    loadGoogleMaps(apiKey)
      .then((maps) => {
        if (cancelled || !mapRef.current) {
          return;
        }

        const appearance = getMapAppearance(resolveTheme());
        const map = new maps.Map(mapRef.current, {
          center: MAP_DEFAULT_CENTER,
          zoom: MAP_DEFAULT_ZOOM,
          styles: appearance.styles,
          disableDefaultUI: true,
          zoomControl: true,
          mapTypeControl: false,
          streetViewControl: false,
          fullscreenControl: false,
          backgroundColor: appearance.backgroundColor,
        });

        const icon = createPinIcon(maps, appearance.pinFill);
        const markers = MAP_CITIES.map((city) => {
          const marker = new maps.Marker({
            map,
            position: { lat: city.lat, lng: city.lng },
            icon,
            clickable: true,
            cursor: 'pointer',
            label: {
              text: city.name,
              color: appearance.labelColor,
              fontSize: '12px',
              fontWeight: '500',
              className: 'relative left-[0.35rem] -top-[0.1rem] whitespace-nowrap',
            },
          });

          bindPinHoverCard({
            maps,
            map,
            marker,
            cityName: city.name,
            onGetWeather: (cityName) => {
              navigate(currentAiWeatherPath(cityName));
            },
          });

          return marker;
        });

        mapsApiRef.current = maps;
        mapInstanceRef.current = map;
        markersRef.current = markers;
        setStatus('ready');
      })
      .catch(() => {
        if (!cancelled) {
          setStatus('error');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [navigate]);

  useEffect(() => {
    const syncMapTheme = (event) => {
      const maps = mapsApiRef.current;
      const map = mapInstanceRef.current;
      if (!maps || !map) {
        return;
      }

      const resolved = event?.detail?.resolved ?? resolveTheme();
      applyMapAppearance(maps, map, markersRef.current, resolved);
    };

    window.addEventListener(THEME_CHANGE_EVENT, syncMapTheme);
    return () => window.removeEventListener(THEME_CHANGE_EVENT, syncMapTheme);
  }, []);

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
