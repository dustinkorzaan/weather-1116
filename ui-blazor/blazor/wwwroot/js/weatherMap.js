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

  const LIGHT_MAP_STYLES = [
    { elementType: 'geometry', stylers: [{ color: '#e8eef4' }] },
    { elementType: 'labels.text.fill', stylers: [{ color: '#4b5563' }] },
    { elementType: 'labels.text.stroke', stylers: [{ color: '#e8eef4' }] },
    {
      featureType: 'administrative',
      elementType: 'geometry.stroke',
      stylers: [{ color: '#cbd5e1' }],
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
      stylers: [{ color: '#4b5563' }],
    },
    { featureType: 'poi', stylers: [{ visibility: 'off' }] },
    { featureType: 'road', stylers: [{ visibility: 'off' }] },
    { featureType: 'transit', stylers: [{ visibility: 'off' }] },
    {
      featureType: 'water',
      elementType: 'geometry',
      stylers: [{ color: '#c5d4e0' }],
    },
    {
      featureType: 'water',
      elementType: 'labels.text.fill',
      stylers: [{ color: '#64748b' }],
    },
  ];

  let loadPromise = null;
  const mapByElement = new WeakMap();
  const themedMaps = [];

  function resolvedTheme() {
    if (window.weatherTheme && typeof window.weatherTheme.resolve === 'function') {
      return window.weatherTheme.resolve(window.weatherTheme.getPreference());
    }
    return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
  }

  function mapAppearance(theme) {
    const isDark = theme === 'dark';
    return {
      styles: isDark ? DARK_MAP_STYLES : LIGHT_MAP_STYLES,
      backgroundColor: isDark ? '#0b111d' : '#e8eef4',
      pinFill: isDark ? '#ffffff' : '#111827',
      labelColor: isDark ? '#e4e4e7' : '#1f2937',
      colorScheme: isDark ? 'DARK' : 'LIGHT',
    };
  }

  function colorSchemeOption(maps, isDark) {
    const schemes = maps.ColorScheme;
    if (schemes) {
      return isDark ? schemes.DARK : schemes.LIGHT;
    }
    return isDark ? 'DARK' : 'LIGHT';
  }

  function applyMapColorSchemeCss(element, theme) {
    if (!element || !element.style) {
      return;
    }
    element.style.colorScheme = theme === 'dark' ? 'dark' : 'light';
  }

  /**
   * Vector maps ignore JSON styles, and colorScheme is init-only. Raster + an
   * explicit LIGHT/DARK scheme keeps the canvas on the site theme.
   */
  function createThemedMap(maps, element, appearance, center, zoom) {
    applyMapColorSchemeCss(element, appearance.colorScheme === 'DARK' ? 'dark' : 'light');
    const options = {
      center: center || DEFAULT_CENTER,
      zoom: zoom == null ? DEFAULT_ZOOM : zoom,
      styles: appearance.styles,
      disableDefaultUI: true,
      zoomControl: true,
      mapTypeControl: false,
      streetViewControl: false,
      fullscreenControl: false,
      backgroundColor: appearance.backgroundColor,
      colorScheme: colorSchemeOption(maps, appearance.colorScheme === 'DARK'),
    };
    if (maps.RenderingType) {
      options.renderingType = maps.RenderingType.RASTER;
    }
    return new maps.Map(element, options);
  }

  function createCityMarkers(maps, map, cities, appearance) {
    const icon = createPinIcon(maps, appearance.pinFill);
    const markers = [];
    (cities || []).forEach(function (city) {
      const marker = new maps.Marker({
        map: map,
        position: { lat: city.lat, lng: city.lng },
        icon: icon,
        clickable: true,
        cursor: 'pointer',
        label: {
          text: city.name,
          color: appearance.labelColor,
          fontSize: '12px',
          fontWeight: '500',
          className: 'weather-map-label',
        },
      });

      bindPinHoverCard(maps, map, marker, city.name, function (cityName) {
        window.location.assign(currentAiWeatherPath(cityName));
      });
      markers.push(marker);
    });
    return markers;
  }
  let activeCloseCard = null;

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

  function currentAiWeatherPath(location) {
    var trimmed = String(location || '').trim();
    if (!trimmed) {
      return '/current-ai-weather';
    }
    return '/current-ai-weather?location=' + encodeURIComponent(trimmed);
  }

  function createPinIcon(maps, pinFill) {
    return {
      path: maps.SymbolPath.CIRCLE,
      scale: 6,
      fillColor: pinFill,
      fillOpacity: 1,
      strokeWeight: 0,
      labelOrigin: new maps.Point(18, 0),
    };
  }

  function applyMapAppearance(entry, theme) {
    const resolved = theme || resolvedTheme();
    if (entry.theme === resolved && entry.map) {
      return;
    }
    const appearance = mapAppearance(resolved);
    let center = DEFAULT_CENTER;
    let zoom = DEFAULT_ZOOM;
    if (entry.map && typeof entry.map.getCenter === 'function') {
      const current = entry.map.getCenter();
      if (current) {
        center = { lat: current.lat(), lng: current.lng() };
      }
      if (typeof entry.map.getZoom === 'function' && entry.map.getZoom() != null) {
        zoom = entry.map.getZoom();
      }
    }

    const map = createThemedMap(entry.maps, entry.element, appearance, center, zoom);
    entry.map = map;
    entry.markers = createCityMarkers(entry.maps, map, entry.cities, appearance);
    entry.theme = resolved;
    if (entry.element) {
      mapByElement.set(entry.element, map);
    }
  }

  function createPinHoverCard(cityName) {
    const card = document.createElement('div');
    card.className = 'weather-map-pin-card';
    card.setAttribute('role', 'dialog');
    card.setAttribute('aria-label', cityName);

    const name = document.createElement('div');
    name.className = 'weather-map-pin-card-name';
    name.textContent = cityName;

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'weather-map-pin-card-button';
    button.textContent = 'Get Current AI Weather';

    card.appendChild(name);
    card.appendChild(button);
    return { card: card, button: button };
  }

  function bindPinHoverCard(maps, map, marker, cityName, onGetWeather) {
    const created = createPinHoverCard(cityName);
    const infoWindow = new maps.InfoWindow({
      content: created.card,
      disableAutoPan: true,
      headerDisabled: true,
    });

    let closeTimer = null;
    let isOpen = false;

    function cancelClose() {
      if (closeTimer !== null) {
        clearTimeout(closeTimer);
        closeTimer = null;
      }
    }

    function openCard() {
      cancelClose();
      if (activeCloseCard && activeCloseCard !== closeCard) {
        activeCloseCard();
      }
      if (!isOpen) {
        infoWindow.open({ map: map, anchor: marker });
        isOpen = true;
      }
      activeCloseCard = closeCard;
    }

    function closeCard() {
      cancelClose();
      if (isOpen) {
        infoWindow.close();
        isOpen = false;
      }
      if (activeCloseCard === closeCard) {
        activeCloseCard = null;
      }
    }

    function scheduleClose() {
      cancelClose();
      closeTimer = setTimeout(closeCard, 200);
    }

    marker.addListener('mouseover', openCard);
    marker.addListener('mouseout', scheduleClose);
    marker.addListener('click', openCard);

    created.card.addEventListener('mouseenter', openCard);
    created.card.addEventListener('mouseleave', scheduleClose);
    created.card.addEventListener('click', function (event) {
      event.stopPropagation();
    });

    created.button.addEventListener('click', function (event) {
      event.preventDefault();
      event.stopPropagation();
      onGetWeather(cityName);
    });

    map.addListener('click', closeCard);
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

      const appearance = mapAppearance(resolvedTheme());
      const map = createThemedMap(maps, current, appearance, DEFAULT_CENTER, DEFAULT_ZOOM);
      const markers = createCityMarkers(maps, map, cities, appearance);
      const entry = {
        maps: maps,
        map: map,
        markers: markers,
        element: current,
        cities: cities || [],
        theme: resolvedTheme(),
      };

      themedMaps.push(entry);
      mapByElement.set(current, map);
      current.setAttribute('data-status', 'ready');
      observeMapSize(current, function () {
        return mapByElement.get(current);
      });
      return map;
    });
  }

  function observeMapSize(element, getMap) {
    if (!window.ResizeObserver || !element || typeof getMap !== 'function') {
      return;
    }

    const observer = new ResizeObserver(function () {
      const map = getMap();
      if (
        map &&
        element.offsetWidth > 0 &&
        element.offsetHeight > 0 &&
        window.google &&
        window.google.maps
      ) {
        window.google.maps.event.trigger(map, 'resize');
      }
    });
    observer.observe(element);
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

  window.addEventListener('weather-theme-change', function (event) {
    const theme = event.detail && event.detail.resolved ? event.detail.resolved : resolvedTheme();
    themedMaps.forEach(function (entry) {
      applyMapAppearance(entry, theme);
    });
  });

  return {
    init: init,
    tryAutoInit: tryAutoInit,
    currentAiWeatherPath: currentAiWeatherPath,
  };
})();
