(function initAddLocation() {
  function cityFromAiWeather(locationInput, data) {
    const lat = Number(data && data.latitude);
    const lng = Number(data && data.longitude);
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
      return null;
    }

    const name = String((data && data.locationName) || locationInput || '').trim();
    if (!name) {
      return null;
    }

    return {
      id: 'pin-' + lat.toFixed(4) + '-' + lng.toFixed(4),
      name: name,
      lat: lat,
      lng: lng,
    };
  }

  document.addEventListener('DOMContentLoaded', function () {
    const wrap = document.getElementById('addLocationWrap');
    const button = document.getElementById('addLocationButton');
    const panel = document.getElementById('addLocationPanel');
    const form = document.getElementById('addLocationForm');
    const input = document.getElementById('addLocationInput');
    const submit = document.getElementById('addLocationSubmit');
    const spinner = document.getElementById('addLocationSpinner');
    const errorEl = document.getElementById('addLocationError');
    const endpoint = form && form.getAttribute('data-ai-weather-url');

    if (!wrap || !button || !panel || !form || !input || !submit || !endpoint) {
      return;
    }

    let isFetching = false;

    function setOpen(open) {
      panel.hidden = !open;
      button.setAttribute('aria-expanded', open ? 'true' : 'false');
      if (open && !isFetching) {
        input.focus();
        input.select();
      }
    }

    function setBusy(busy) {
      isFetching = busy;
      input.disabled = busy;
      submit.disabled = busy;
      submit.setAttribute('aria-busy', busy ? 'true' : 'false');
      if (spinner) {
        spinner.hidden = !busy;
      }
      submit.querySelector('.add-location-submit-label').textContent = busy
        ? 'Looking up weather…'
        : 'Add to map';
    }

    button.addEventListener('click', function (event) {
      event.stopPropagation();
      if (isFetching) {
        setOpen(true);
        return;
      }
      setOpen(panel.hidden);
      if (errorEl) {
        errorEl.hidden = true;
      }
    });

    document.addEventListener('click', function (event) {
      if (isFetching || panel.hidden) {
        return;
      }
      if (!wrap.contains(event.target)) {
        setOpen(false);
      }
    });

    document.addEventListener('keydown', function (event) {
      if (event.key === 'Escape' && !isFetching) {
        setOpen(false);
      }
    });

    form.addEventListener('submit', function (event) {
      event.preventDefault();
      const location = (input.value || '').trim() || 'Nashville, TN';
      input.value = location;
      if (errorEl) {
        errorEl.hidden = true;
        errorEl.textContent = '';
      }
      setBusy(true);

      fetch(endpoint + (endpoint.indexOf('?') >= 0 ? '&' : '?') + 'location=' + encodeURIComponent(location), {
        headers: { Accept: 'application/json' },
      })
        .then(function (response) {
          if (!response.ok) {
            throw new Error('Unable to load AI weather.');
          }
          return response.json();
        })
        .then(function (data) {
          const city = cityFromAiWeather(location, data);
          if (!city) {
            throw new Error('AI weather did not include map coordinates.');
          }
          if (window.weatherMap && typeof window.weatherMap.addCity === 'function') {
            window.weatherMap.addCity(city);
          } else {
            try {
              const key = 'weather-map-cities';
              const raw = window.sessionStorage.getItem(key);
              const cities = raw ? JSON.parse(raw) : null;
              const list = Array.isArray(cities) ? cities : [
                { id: '59e2459a-b25d-44a7-bcb0-2a4f2e444272', name: 'New York, NY', lat: 40.7128, lng: -74.006 },
                { id: '329735f1-cfc0-42b4-a48f-0d41677145e8', name: 'Toronto, ON', lat: 43.6532, lng: -79.3832 },
                { id: '9daab691-7885-400f-8aed-5e21a63f9a7a', name: 'Atlanta, GA', lat: 33.749, lng: -84.388 },
                { id: '04f5d22f-ca31-4d29-ac9e-a1c4f0127ed1', name: 'Charlotte, NC', lat: 35.2271, lng: -80.8431 },
              ];
              if (!list.some(function (item) { return item.id === city.id; })) {
                list.push(city);
              }
              window.sessionStorage.setItem(key, JSON.stringify(list));
            } catch (e) {
              // Ignore storage failures; the pin appears after returning home if the map is loaded.
            }
          }
          setOpen(false);
          input.value = 'Nashville, TN';
        })
        .catch(function (error) {
          if (errorEl) {
            errorEl.textContent = error && error.message ? error.message : 'Unable to load AI weather.';
            errorEl.hidden = false;
          }
        })
        .finally(function () {
          setBusy(false);
        });
    });
  });
})();
