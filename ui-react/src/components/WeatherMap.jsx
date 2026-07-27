import { useEffect, useRef, useState } from 'react';
import {
  MAP_CITIES,
  MAP_DEFAULT_CENTER,
  MAP_DEFAULT_ZOOM,
} from '../data/mapCities';
import { DARK_MAP_STYLES } from '../map/darkMapStyles';
import { loadGoogleMaps } from '../map/loadGoogleMaps';

const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '';

function createWhiteDotIcon(maps) {
  return {
    path: maps.SymbolPath.CIRCLE,
    scale: 6,
    fillColor: '#ffffff',
    fillOpacity: 1,
    strokeWeight: 0,
    labelOrigin: new maps.Point(18, 0),
  };
}

function WeatherMap() {
  const mapRef = useRef(null);
  const mapInstanceRef = useRef(null);
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

        const map = new maps.Map(mapRef.current, {
          center: MAP_DEFAULT_CENTER,
          zoom: MAP_DEFAULT_ZOOM,
          styles: DARK_MAP_STYLES,
          disableDefaultUI: true,
          zoomControl: true,
          mapTypeControl: false,
          streetViewControl: false,
          fullscreenControl: false,
          backgroundColor: '#0b111d',
        });

        const icon = createWhiteDotIcon(maps);
        MAP_CITIES.forEach((city) => {
          new maps.Marker({
            map,
            position: { lat: city.lat, lng: city.lng },
            title: city.name,
            icon,
            label: {
              text: city.name,
              color: '#e4e4e7',
              fontSize: '12px',
              fontWeight: '500',
              className: 'weather-map-label',
            },
          });
        });

        mapInstanceRef.current = map;
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
  }, []);

  return (
    <section className="weather-map-section" aria-label="Map">
      {!apiKey && (
        <p className="forecast-status error">
          Set <code>VITE_GOOGLE_MAPS_API_KEY</code> to enable Google Maps.
        </p>
      )}
      {status === 'error' && (
        <p className="forecast-status error">
          Unable to load Google Maps. Check the API key and that Maps JavaScript API is enabled.
        </p>
      )}
      <div
        ref={mapRef}
        className="weather-map"
        role="presentation"
        data-status={status}
      />
    </section>
  );
}

export default WeatherMap;
