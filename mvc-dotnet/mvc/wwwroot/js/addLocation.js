(function initAddLocation() {
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

  function cityFromLatLongSearch(locationInput, data) {
    const lat = Number(data && data.latitude);
    const lng = Number(data && data.longitude);
    if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
      return null;
    }

    const parts = [data && data.name, data && data.state]
      .map(function (part) { return String(part || '').trim(); })
      .filter(Boolean);
    const name = parts.join(', ') || String(locationInput || '').trim();
    if (!name) {
      return null;
    }

    return {
      id: newCityId(),
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
    const endpoint = form && form.getAttribute('data-geo-url');

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
        ? 'Looking up location…'
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
            throw new Error('Unable to find that location.');
          }
          return response.json();
        })
        .then(function (data) {
          const city = cityFromLatLongSearch(location, data);
          if (!city) {
            throw new Error('Unable to find that location.');
          }
          // Off the Home page there is no map, so save the pin directly; Home reads it from /User.
          if (window.weatherMap && typeof window.weatherMap.addCity === 'function') {
            return window.weatherMap.addCity(city);
          }
          return fetch('/User/AddPin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
            body: JSON.stringify({ latitude: city.lat, longitude: city.lng, locationName: city.name }),
          }).then(function (response) {
            if (!response.ok) {
              throw new Error('Unable to save that location.');
            }
          });
        })
        .then(function () {
          setOpen(false);
          input.value = 'Nashville, TN';
        })
        .catch(function (error) {
          if (errorEl) {
            errorEl.textContent = error && error.message ? error.message : 'Unable to find that location.';
            errorEl.hidden = false;
          }
        })
        .finally(function () {
          setBusy(false);
        });
    });
  });
})();
