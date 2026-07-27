window.weatherMap = (function () {
  const DEFAULT_CENTER = { lat: 39.5, lng: -77.5 };
  const DEFAULT_ZOOM = 5;

  const DARK_MAP_STYLES = [
    { elementType: 'geometry', stylers: [{ color: '#0b111d' }] },
    { elementType: 'labels.text.fill', stylers: [{ color: '#a1a1aa' }] },
    { elementType: 'labels.text.stroke', stylers: [{ color: '#0b111d' }] },
    {
      featureType: 'administrative',
      elementType: 'geometry.stroke',
      stylers: [{ color: '#3f3f46' }],
    },
    {
      featureType: 'administrative.land_parcel',
      stylers: [{ visibility: 'off' }],
    },
    {
      featureType: 'administrative.neighborhood',
      stylers: [{ visibility: 'off' }],
    },
    {
      featureType: 'administrative.province',
      elementType: 'labels.text.fill',
      stylers: [{ color: '#a1a1aa' }],
    },
    { featureType: 'poi', stylers: [{ visibility: 'off' }] },
    { featureType: 'road', stylers: [{ visibility: 'off' }] },
    { featureType: 'transit', stylers: [{ visibility: 'off' }] },
    {
      featureType: 'water',
      elementType: 'geometry',
      stylers: [{ color: '#060a12' }],
    },
    {
      featureType: 'water',
      elementType: 'labels.text.fill',
      stylers: [{ color: '#52525b' }],
    },
  ];

  let loadPromise = null;

  function loadGoogleMaps(apiKey) {
    if (window.google && window.google.maps) {
      return Promise.resolve(window.google.maps);
    }

    if (loadPromise) {
      return loadPromise;
    }

    if (!apiKey) {
      return Promise.reject(new Error('Missing Google Maps API key.'));
    }

    loadPromise = new Promise(function (resolve, reject) {
      const script = document.createElement('script');
      script.src =
        'https://maps.googleapis.com/maps/api/js?key=' +
        encodeURIComponent(apiKey);
      script.async = true;
      script.defer = true;
      script.onload = function () {
        if (window.google && window.google.maps) {
          resolve(window.google.maps);
        } else {
          loadPromise = null;
          reject(new Error('Google Maps failed to initialize.'));
        }
      };
      script.onerror = function () {
        loadPromise = null;
        reject(new Error('Failed to load the Google Maps script.'));
      };
      document.head.appendChild(script);
    });

    return loadPromise;
  }

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

  function resolveElement(elementOrId) {
    if (typeof elementOrId === 'string') {
      return document.getElementById(elementOrId);
    }
    return elementOrId;
  }

  /**
   * @param {string|HTMLElement} elementOrId
   * @param {string} apiKey
   * @param {Array<{id:string,name:string,lat:number,lng:number}>} cities
   */
  function init(elementOrId, apiKey, cities) {
    const element = resolveElement(elementOrId);
    if (!element) {
      return Promise.reject(new Error('Map container not found.'));
    }

    if (!apiKey) {
      element.setAttribute('data-status', 'missing-key');
      return Promise.reject(new Error('Missing Google Maps API key.'));
    }

    element.setAttribute('data-status', 'loading');

    return loadGoogleMaps(apiKey).then(function (maps) {
      const map = new maps.Map(element, {
        center: DEFAULT_CENTER,
        zoom: DEFAULT_ZOOM,
        styles: DARK_MAP_STYLES,
        disableDefaultUI: true,
        zoomControl: true,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
        backgroundColor: '#0b111d',
      });

      const icon = createWhiteDotIcon(maps);
      (cities || []).forEach(function (city) {
        new maps.Marker({
          map: map,
          position: { lat: city.lat, lng: city.lng },
          title: city.name,
          icon: icon,
          label: {
            text: city.name,
            color: '#e4e4e7',
            fontSize: '12px',
            fontWeight: '500',
            className: 'weather-map-label',
          },
        });
      });

      element.setAttribute('data-status', 'ready');
      return map;
    });
  }

  return { init: init };
})();
