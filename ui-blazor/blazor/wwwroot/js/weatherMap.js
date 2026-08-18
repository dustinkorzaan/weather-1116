window.weatherMap = (function () {
  const DEFAULT_CITIES = [
    { id: '59e2459a-b25d-44a7-bcb0-2a4f2e444272', name: 'New York, NY', lat: 40.7128, lng: -74.006 },
    { id: '329735f1-cfc0-42b4-a48f-0d41677145e8', name: 'Toronto, ON', lat: 43.6532, lng: -79.3832 },
    { id: '9daab691-7885-400f-8aed-5e21a63f9a7a', name: 'Atlanta, GA', lat: 33.749, lng: -84.388 },
    { id: '04f5d22f-ca31-4d29-ac9e-a1c4f0127ed1', name: 'Charlotte, NC', lat: 35.2271, lng: -80.8431 },
  ];
  const STORAGE_KEY = 'weather-map-cities';
  const DELETE_ICON_SVG =
    '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18"/><path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"/><path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"/><line x1="10" x2="10" y1="11" y2="17"/><line x1="14" x2="14" y1="11" y2="17"/></svg>';
  const PLUS_ICON_SVG =
    '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 5v14"/><path d="M5 12h14"/></svg>';
  const SEARCH_ICON_SVG =
    '<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="11" cy="11" r="8" /><path d="m21 21-4.3-4.3" /></svg>';
  const DEFAULT_CENTER = { lat: 36.16, lng: -86.78 };
  const DEFAULT_ZOOM = 4;

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

  function defaultMapTypeId(maps, mapTypeId, isDark) {
    if (mapTypeId) {
      return mapTypeId;
    }
    if (isDark) {
      return (maps && maps.MapTypeId && maps.MapTypeId.ROADMAP) || 'roadmap';
    }
    return (maps && maps.MapTypeId && maps.MapTypeId.HYBRID) || 'hybrid';
  }

  function createMapTypeControlOptions(maps) {
    const ids = maps && maps.MapTypeId;
    const options = {
      mapTypeIds: [
        (ids && ids.ROADMAP) || 'roadmap',
        (ids && ids.SATELLITE) || 'satellite',
        (ids && ids.HYBRID) || 'hybrid',
        (ids && ids.TERRAIN) || 'terrain',
      ],
    };
    if (maps && maps.MapTypeControlStyle && maps.MapTypeControlStyle.HORIZONTAL_BAR) {
      options.style = maps.MapTypeControlStyle.HORIZONTAL_BAR;
    }
    if (maps && maps.ControlPosition && maps.ControlPosition.TOP_LEFT) {
      options.position = maps.ControlPosition.TOP_LEFT;
    }
    return options;
  }

  /**
   * Vector maps ignore JSON styles, and colorScheme is init-only. Raster + an
   * explicit LIGHT/DARK scheme keeps the canvas on the site theme.
   */
  function createThemedMap(maps, element, appearance, center, zoom, mapTypeId, onTypeChanged) {
    applyMapColorSchemeCss(element, appearance.colorScheme === 'DARK' ? 'dark' : 'light');
    const options = {
      center: center || DEFAULT_CENTER,
      zoom: zoom == null ? DEFAULT_ZOOM : zoom,
      mapTypeId: defaultMapTypeId(maps, mapTypeId, appearance.colorScheme === 'DARK'),
      mapTypeControl: true,
      mapTypeControlOptions: createMapTypeControlOptions(maps),
      styles: appearance.styles,
      disableDefaultUI: true,
      zoomControl: true,
      streetViewControl: false,
      fullscreenControl: false,
      backgroundColor: appearance.backgroundColor,
      colorScheme: colorSchemeOption(maps, appearance.colorScheme === 'DARK'),
    };
    if (maps.RenderingType) {
      options.renderingType = maps.RenderingType.RASTER;
    }
    const map = new maps.Map(element, options);
    if (typeof onTypeChanged === 'function' && typeof map.addListener === 'function') {
      map.addListener('maptypeid_changed', function () {
        if (typeof map.getMapTypeId === 'function') {
          onTypeChanged(map.getMapTypeId());
        }
      });
    }
    return map;
  }

  function logoPinUrl(theme) {
    return theme === 'dark' ? '/logo-solid.svg' : '/logo-black-solid.svg';
  }

  function logoPinSpinOffsetSec(index) {
    return ((Number(index) || 0) * 3.7) % 16;
  }

  function bindPinDomEvent(element, mapsEventName, handler) {
    const domEvent =
      mapsEventName === 'mouseover' ? 'mouseenter' : mapsEventName === 'mouseout' ? 'mouseleave' : mapsEventName;
    element.addEventListener(domEvent, handler);
  }

  function createLogoPinOverlayClass(maps) {
    class LogoPinOverlay extends maps.OverlayView {
      constructor(options) {
        super();
        this.position = new maps.LatLng(options.lat, options.lng);
        if (typeof this.set === 'function') {
          this.set('position', this.position);
        }
        this.cityName = options.cityName;
        this.logoUrl = options.logoUrl;
        this.spinOffsetSec = options.spinOffsetSec || 0;
        this.pendingListeners = [];
        this.div = null;
      }

      onAdd() {
        const pin = document.createElement('button');
        pin.type = 'button';
        pin.className = 'weather-map-logo-pin';
        pin.setAttribute('aria-label', this.cityName);

        const pulse = document.createElement('span');
        pulse.className = 'weather-map-logo-pin-pulse';
        pulse.setAttribute('aria-hidden', 'true');

        const spinner = document.createElement('span');
        spinner.className = 'weather-map-logo-pin-spin';
        spinner.style.animationDelay = '-' + this.spinOffsetSec + 's';

        const image = document.createElement('img');
        image.className = 'weather-map-logo-pin-image';
        image.src = this.logoUrl;
        image.alt = '';
        image.draggable = false;

        spinner.appendChild(image);
        pin.appendChild(pulse);
        pin.appendChild(spinner);
        pin.addEventListener('click', function (event) {
          event.stopPropagation();
        });
        pin.addEventListener('mousedown', function (event) {
          event.stopPropagation();
        });

        this.div = pin;
        const panes = this.getPanes();
        if (!panes || !panes.overlayMouseTarget) {
          return;
        }
        panes.overlayMouseTarget.appendChild(pin);
        this.pendingListeners.forEach(function (item) {
          bindPinDomEvent(pin, item.eventName, item.handler);
        });
        this.pendingListeners = [];
      }

      draw() {
        const projection = this.getProjection();
        if (!projection || !this.div) {
          return;
        }
        const point = projection.fromLatLngToDivPixel(this.position);
        if (!point) {
          return;
        }
        this.div.style.left = point.x + 'px';
        this.div.style.top = point.y + 'px';
      }

      onRemove() {
        if (this.div && this.div.parentNode) {
          this.div.parentNode.removeChild(this.div);
        }
        this.div = null;
      }

      getPosition() {
        return this.position;
      }

      addListener(eventName, handler) {
        if (this.div) {
          bindPinDomEvent(this.div, eventName, handler);
          return;
        }
        this.pendingListeners.push({ eventName: eventName, handler: handler });
      }

      setLogoUrl(url) {
        this.logoUrl = url;
        const image = this.div && this.div.querySelector('.weather-map-logo-pin-image');
        if (image) {
          image.src = url;
        }
      }
    }

    return LogoPinOverlay;
  }

  function createCityMarkers(maps, map, cities, appearance) {
    const LogoPinOverlay = createLogoPinOverlayClass(maps);
    const logoUrl = logoPinUrl(appearance.colorScheme === 'DARK' ? 'dark' : 'light');
    const markers = [];
    (cities || []).forEach(function (city, index) {
      markers.push(createOneMarker(maps, map, city, appearance, index, LogoPinOverlay, logoUrl));
    });
    return markers;
  }

  function createOneMarker(maps, map, city, appearance, index, LogoPinOverlay, logoUrl) {
    const OverlayClass = LogoPinOverlay || createLogoPinOverlayClass(maps);
    const pinUrl = logoUrl || logoPinUrl(appearance.colorScheme === 'DARK' ? 'dark' : 'light');
    const overlay = new OverlayClass({
      lat: city.lat,
      lng: city.lng,
      cityName: city.name,
      logoUrl: pinUrl,
      spinOffsetSec: logoPinSpinOffsetSec(index),
    });
    overlay.cityId = city.id;
    overlay.setMap(map);

    bindPinHoverCard(
      maps,
      map,
      overlay,
      city.name,
      function () {
        navigateToWeather(city.name, city.lat, city.lng, 'current');
      },
      function () {
        removeCity(city.id);
      }
    );
    return overlay;
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

  function weatherModalPath(name, lat, lng, tab) {
    var params = new URLSearchParams();
    var trimmedName = String(name || '').trim();
    if (trimmedName) {
      params.set('name', trimmedName);
    }
    if (Number.isFinite(Number(lat))) {
      params.set('lat', String(lat));
    }
    if (Number.isFinite(Number(lng))) {
      params.set('lng', String(lng));
    }
    params.set('tab', tab || 'current');
    return '/weather?' + params.toString();
  }

  /**
   * Uses Blazor's client-side router (already connected on this page) instead
   * of a full browser navigation, so the pin click reuses the live circuit
   * rather than round-tripping through a fresh server request.
   */
  function navigateToWeather(name, lat, lng, tab) {
    const path = weatherModalPath(name, lat, lng, tab);
    if (window.Blazor && typeof window.Blazor.navigateTo === 'function') {
      window.Blazor.navigateTo(path);
    } else {
      window.location.assign(path);
    }
  }

  function newCityId() {
    if (window.crypto && typeof window.crypto.randomUUID === 'function') {
      return window.crypto.randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (char) {
      const nibble = (Math.random() * 16) | 0;
      const value = char === 'x' ? nibble : (nibble & 0x3) | 0x8;
      return value.toString(16);
    });
  }

  function resolveGetLocationUrl(element) {
    const fromElement = element && element.getAttribute && element.getAttribute('data-get-location-url');
    return fromElement || '/Geo/GetLocation';
  }

  function buildGetLocationUrl(baseUrl, lat, lng) {
    const separator = String(baseUrl || '').indexOf('?') >= 0 ? '&' : '?';
    return (
      baseUrl +
      separator +
      'latitude=' +
      encodeURIComponent(String(lat)) +
      '&longitude=' +
      encodeURIComponent(String(lng))
    );
  }

  function createAddLocationCard() {
    const card = document.createElement('div');
    card.className = 'weather-map-add-location weather-map-pin-card';
    card.setAttribute('role', 'dialog');
    card.setAttribute('aria-label', 'Add Location');

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'weather-map-add-location-button weather-map-pin-card-button';
    button.setAttribute('aria-label', 'Add Location');

    const icon = document.createElement('span');
    icon.className = 'weather-map-add-location-icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.innerHTML = PLUS_ICON_SVG;

    const label = document.createElement('span');
    label.className = 'weather-map-add-location-label';
    label.textContent = 'Add Location';

    button.appendChild(icon);
    button.appendChild(label);

    const error = document.createElement('p');
    error.className = 'weather-map-add-location-error';
    error.hidden = true;

    card.appendChild(button);
    card.appendChild(error);

    function setBusy(busy) {
      button.disabled = busy;
      button.setAttribute('aria-busy', busy ? 'true' : 'false');
      label.textContent = busy ? 'Looking up location…' : 'Add Location';
    }

    function setError(message) {
      const text = String(message || '').trim();
      error.textContent = text;
      error.hidden = !text;
    }

    return { card: card, button: button, setBusy: setBusy, setError: setError };
  }

  function lookupAndAddFromLatLng(lat, lng, getLocationUrl, controls) {
    controls.setError('');
    controls.setBusy(true);

    fetch(buildGetLocationUrl(getLocationUrl, lat, lng), {
      headers: { Accept: 'application/json' },
    })
      .then(function (response) {
        if (!response.ok) {
          throw new Error('Unable to find that location.');
        }
        return response.json();
      })
      .then(function (data) {
        const name = String((data && data.location) || '').trim();
        if (!name) {
          throw new Error('Unable to find that location.');
        }
        addCity({
          id: newCityId(),
          name: name,
          lat: lat,
          lng: lng,
        });
        controls.hide();
      })
      .catch(function (error) {
        controls.setError(error && error.message ? error.message : 'Unable to find that location.');
      })
      .finally(function () {
        controls.setBusy(false);
      });
  }

  function bindRightClickAddLocation(maps, map, getLocationUrl) {
    let infoWindow = null;
    let isOpen = false;

    function hide() {
      if (infoWindow && isOpen) {
        infoWindow.close();
      }
      isOpen = false;
      infoWindow = null;
    }

    map.addListener('rightclick', function (event) {
      const latLng = event && event.latLng;
      if (!latLng || typeof latLng.lat !== 'function' || typeof latLng.lng !== 'function') {
        return;
      }

      hide();

      const lat = latLng.lat();
      const lng = latLng.lng();
      const created = createAddLocationCard();
      const infoWindowOptions = {
        content: created.card,
        position: latLng,
        disableAutoPan: true,
        headerDisabled: true,
      };
      if (typeof maps.Size === 'function') {
        infoWindowOptions.pixelOffset = new maps.Size(12, -8);
      }
      infoWindow = new maps.InfoWindow(infoWindowOptions);
      infoWindow.open({ map: map });
      isOpen = true;

      created.card.addEventListener('click', function (clickEvent) {
        clickEvent.stopPropagation();
      });
      created.card.addEventListener('mousedown', function (mouseEvent) {
        mouseEvent.stopPropagation();
      });
      created.button.addEventListener('click', function (clickEvent) {
        clickEvent.preventDefault();
        clickEvent.stopPropagation();
        lookupAndAddFromLatLng(lat, lng, getLocationUrl, {
          setBusy: created.setBusy,
          setError: created.setError,
          hide: hide,
        });
      });
    });

    map.addListener('click', hide);
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

    if (entry.markers) {
      entry.markers.forEach(function (pin) {
        if (pin && typeof pin.setMap === 'function') {
          pin.setMap(null);
        }
      });
    }

    // entry.mapTypeId is only set when the user explicitly picks a type via
    // the map type control (see the maptypeid_changed listener below), so an
    // untouched map keeps re-deriving the theme's default (Map for dark,
    // Hybrid for light) on every theme switch instead of getting stuck on
    // whichever default happened to render first.
    const map = createThemedMap(entry.maps, entry.element, appearance, center, zoom, entry.mapTypeId, function (typeId) {
      entry.mapTypeId = typeId;
    });
    entry.map = map;
    entry.markers = createCityMarkers(entry.maps, map, entry.cities, appearance);
    entry.theme = resolved;
    if (entry.element) {
      mapByElement.set(entry.element, map);
    }
    bindRightClickAddLocation(entry.maps, map, entry.getLocationUrl || '/Geo/GetLocation');
  }

  function createPinHoverCard(cityName) {
    const card = document.createElement('div');
    card.className = 'weather-map-pin-card';
    card.setAttribute('role', 'dialog');
    card.setAttribute('aria-label', cityName);

    const header = document.createElement('div');
    header.className = 'weather-map-pin-card-header';

    const name = document.createElement('div');
    name.className = 'weather-map-pin-card-name';
    name.textContent = cityName;

    const deleteButton = document.createElement('button');
    deleteButton.type = 'button';
    deleteButton.className = 'weather-map-pin-card-delete';
    deleteButton.setAttribute('aria-label', 'Remove ' + cityName + ' from the map');
    deleteButton.innerHTML = DELETE_ICON_SVG;

    header.appendChild(name);
    header.appendChild(deleteButton);

    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'weather-map-pin-card-button';
    button.innerHTML = SEARCH_ICON_SVG + '<span>Weather</span>';

    card.appendChild(header);
    card.appendChild(button);
    return { card: card, button: button, deleteButton: deleteButton };
  }

  function bindPinHoverCard(maps, map, marker, cityName, onGetWeather, onDelete) {
    const created = createPinHoverCard(cityName);
    const infoWindowOptions = {
      content: created.card,
      disableAutoPan: true,
      headerDisabled: true,
    };
    if (typeof maps.Size === 'function') {
      infoWindowOptions.pixelOffset = new maps.Size(0, -18);
    }
    const infoWindow = new maps.InfoWindow(infoWindowOptions);

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

    created.deleteButton.addEventListener('click', function (event) {
      event.preventDefault();
      event.stopPropagation();
      closeCard();
      if (typeof onDelete === 'function') {
        onDelete();
      }
    });

    map.addListener('click', closeCard);
  }

  function isValidCity(city) {
    return !!(
      city &&
      city.id &&
      city.name &&
      Number.isFinite(city.lat) &&
      Number.isFinite(city.lng)
    );
  }

  function loadStoredCities() {
    try {
      const raw = window.sessionStorage && window.sessionStorage.getItem(STORAGE_KEY);
      if (raw == null) {
        return null;
      }
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed) && parsed.every(isValidCity)) {
        return parsed;
      }
    } catch (e) {
      return null;
    }
    return null;
  }

  function saveCities(cities) {
    try {
      if (window.sessionStorage) {
        window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(cities));
      }
    } catch (e) {
      // Ignore quota / private-mode failures.
    }
  }

  function ensureCities(fallback) {
    const stored = loadStoredCities();
    if (stored) {
      return stored.slice();
    }
    const seed = ((fallback && fallback.length ? fallback : DEFAULT_CITIES) || []).map(function (city) {
      return { id: city.id, name: city.name, lat: city.lat, lng: city.lng };
    });
    saveCities(seed);
    return seed;
  }

  function mapEntries() {
    return themedMaps;
  }

  function addCity(city) {
    if (!isValidCity(city)) {
      return;
    }
    const nextCity = { id: city.id, name: city.name, lat: city.lat, lng: city.lng };
    const cities = ensureCities(DEFAULT_CITIES);
    const exists = cities.some(function (item) {
      return item.id === nextCity.id || (item.lat === nextCity.lat && item.lng === nextCity.lng);
    });
    if (!exists) {
      cities.push(nextCity);
      saveCities(cities);
    }
    mapEntries().forEach(function (entry) {
      entry.cities = cities.slice();
      const already = (entry.markers || []).some(function (pin) {
        return pin.cityId === nextCity.id;
      });
      if (already || !entry.map || !entry.maps) {
        return;
      }
      const appearance = mapAppearance(entry.theme || resolvedTheme());
      entry.markers = (entry.markers || []).concat(
        createOneMarker(entry.maps, entry.map, nextCity, appearance, entry.markers.length)
      );
      if (typeof entry.map.panTo === 'function') {
        entry.map.panTo({ lat: nextCity.lat, lng: nextCity.lng });
      }
    });
  }

  function removeCity(cityId) {
    const cities = ensureCities(DEFAULT_CITIES).filter(function (city) {
      return city.id !== cityId;
    });
    saveCities(cities);
    mapEntries().forEach(function (entry) {
      entry.cities = cities.slice();
      const remaining = [];
      (entry.markers || []).forEach(function (pin) {
        if (pin.cityId === cityId) {
          if (pin && typeof pin.setMap === 'function') {
            pin.setMap(null);
          }
        } else {
          remaining.push(pin);
        }
      });
      entry.markers = remaining;
    });
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
      const resolvedCities = ensureCities(cities);
      const entry = {
        maps: maps,
        map: null,
        markers: null,
        element: current,
        cities: resolvedCities,
        theme: resolvedTheme(),
        getLocationUrl: resolveGetLocationUrl(current),
      };
      const map = createThemedMap(maps, current, appearance, DEFAULT_CENTER, DEFAULT_ZOOM, undefined, function (typeId) {
        entry.mapTypeId = typeId;
      });
      const markers = createCityMarkers(maps, map, resolvedCities, appearance);
      const getLocationUrl = entry.getLocationUrl;
      entry.map = map;
      entry.markers = markers;
      bindRightClickAddLocation(maps, map, getLocationUrl);

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
    addCity: addCity,
    removeCity: removeCity,
    weatherModalPath: weatherModalPath,
  };
})();
