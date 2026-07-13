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
  const mapByElement = new WeakMap();

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
        encodeURIComponent(apiKey) +
        '&loading=async';
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

    if (mapByElement.has(element) || element.getAttribute('data-status') === 'ready') {
      return Promise.resolve(mapByElement.get(element));
    }

    if (!apiKey) {
      element.setAttribute('data-status', 'missing-key');
      return Promise.reject(new Error('Missing Google Maps API key.'));
    }

    element.setAttribute('data-status', 'loading');

    return loadGoogleMaps(apiKey).then(function (maps) {
      // Blazor may replace the node between schedule and resolve; re-check.
      const current = resolveElement(element.id || elementOrId) || element;
      if (!current.isConnected) {
        return null;
      }

      if (mapByElement.has(current)) {
        return mapByElement.get(current);
      }

      const map = new maps.Map(current, {
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

      mapByElement.set(current, map);
      current.setAttribute('data-status', 'ready');
      return map;
    });
  }

  function parseCities(raw) {
    if (!raw) {
      return [];
    }
    try {
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch (e) {
      return [];
    }
  }

  function tryAutoInit(root) {
    const scope = root && root.querySelectorAll ? root : document;
    const elements = [];

    if (scope.getElementById) {
      const byId = scope.getElementById('weather-map');
      if (byId) {
        elements.push(byId);
      }
    }

    scope.querySelectorAll('[data-google-maps-key]').forEach(function (el) {
      if (elements.indexOf(el) === -1) {
        elements.push(el);
      }
    });

    elements.forEach(function (el) {
      const status = el.getAttribute('data-status');
      // Skip containers that are already initializing, ready, or known missing a key.
      if (status === 'ready' || status === 'loading' || status === 'missing-key') {
        return;
      }

      const apiKey = el.getAttribute('data-google-maps-key') || '';
      if (!apiKey) {
        el.setAttribute('data-status', 'missing-key');
        return;
      }

      init(el, apiKey, parseCities(el.getAttribute('data-cities'))).catch(function () {
        if (el.isConnected) {
          el.setAttribute('data-status', 'error');
        }
      });
    });
  }

  function startAutoInit() {
    tryAutoInit(document);

    if (window.MutationObserver) {
      const observer = new MutationObserver(function (mutations) {
        for (let i = 0; i < mutations.length; i++) {
          const mutation = mutations[i];
          if (mutation.addedNodes && mutation.addedNodes.length) {
            tryAutoInit(document);
            return;
          }
        }
      });
      observer.observe(document.documentElement, { childList: true, subtree: true });
    }

    // Blazor circuit connect can replace prerendered nodes shortly after load.
    window.setTimeout(function () {
      tryAutoInit(document);
    }, 500);
    window.setTimeout(function () {
      tryAutoInit(document);
    }, 2000);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', startAutoInit);
  } else {
    startAutoInit();
  }

  return { init: init, tryAutoInit: tryAutoInit };
})();
