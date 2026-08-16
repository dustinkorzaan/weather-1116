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

  function currentAiWeatherPath(location) {
    var trimmed = String(location || '').trim();
    if (!trimmed) {
      return '/current-ai-weather';
    }
    return '/current-ai-weather?location=' + encodeURIComponent(trimmed);
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
        const marker = new maps.Marker({
          map: map,
          position: { lat: city.lat, lng: city.lng },
          icon: icon,
          clickable: true,
          cursor: 'pointer',
          label: {
            text: city.name,
            color: '#e4e4e7',
            fontSize: '12px',
            fontWeight: '500',
            className: 'weather-map-label',
          },
        });

        bindPinHoverCard(maps, map, marker, city.name, function (cityName) {
          window.location.assign(currentAiWeatherPath(cityName));
        });
      });

      element.setAttribute('data-status', 'ready');
      return map;
    });
  }

  return {
    init: init,
    currentAiWeatherPath: currentAiWeatherPath,
  };
})();
