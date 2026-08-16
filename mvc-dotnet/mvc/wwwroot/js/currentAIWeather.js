(function (window) {
  'use strict';

  function locationParts(location) {
    return String(location || '')
      .split(/[,\s]+/)
      .map(function (part) {
        return part.trim();
      })
      .filter(Boolean);
  }

  function locationSearchValue(location) {
    var parts = locationParts(location);
    if (parts.length >= 2 && parts[parts.length - 1].length === 2) {
      return parts.slice(0, -1).join(' ') + ', ' + parts[parts.length - 1].toUpperCase();
    }
    return parts.join(' ');
  }

  function setHidden(el, hidden) {
    if (!el) {
      return;
    }
    if (hidden) {
      el.setAttribute('hidden', '');
    } else {
      el.removeAttribute('hidden');
    }
  }

  function init(config) {
    var form = document.getElementById(config.formId);
    var locationInput = document.getElementById(config.locationId);
    var button = document.getElementById(config.buttonId);
    var spinner = document.getElementById(config.spinnerId);
    var errorEl = document.getElementById(config.errorId);
    var resultsEl = document.getElementById(config.resultsId);
    var summaryEl = document.getElementById(config.summaryId);
    var temperatureEl = document.getElementById(config.temperatureId);
    var windSpeedEl = document.getElementById(config.windSpeedId);
    var windDirectionEl = document.getElementById(config.windDirectionId);
    var conditionsEl = document.getElementById(config.conditionsId);

    if (!form || !locationInput || !button) {
      return;
    }

    function requestWeather() {
      var location = (locationInput.value || '').trim() || 'Nashville, TN';
      locationInput.value = location;

      setHidden(errorEl, true);
      setHidden(resultsEl, true);
      setHidden(spinner, false);
      button.disabled = true;
      button.setAttribute('aria-busy', 'true');
      locationInput.disabled = true;

      var url = config.endpoint + (config.endpoint.indexOf('?') >= 0 ? '&' : '?') +
        'location=' + encodeURIComponent(location);

      fetch(url, { headers: { Accept: 'application/json' } })
        .then(function (response) {
          if (!response.ok) {
            throw new Error('Unable to load AI weather.');
          }
          return response.json();
        })
        .then(function (data) {
          summaryEl.textContent = data.fullSummary || '';
          temperatureEl.textContent = data.temperatureF;
          windSpeedEl.textContent = data.windSpeedMPH;
          windDirectionEl.textContent = data.windDirection || '';
          conditionsEl.textContent = data.conditions || '';
          setHidden(resultsEl, false);
        })
        .catch(function () {
          errorEl.textContent = 'Unable to load AI weather.';
          setHidden(errorEl, false);
        })
        .finally(function () {
          setHidden(spinner, true);
          button.disabled = false;
          button.removeAttribute('aria-busy');
          locationInput.disabled = false;
        });
    }

    function consumeLocationQuery() {
      var params = new URLSearchParams(window.location.search);
      var fromQuery = locationSearchValue(params.get('location'));
      if (!fromQuery) {
        return false;
      }

      locationInput.value = fromQuery;
      if (window.history && window.history.replaceState) {
        window.history.replaceState({}, '', window.location.pathname + window.location.hash);
      }
      return true;
    }

    form.addEventListener('submit', function (event) {
      event.preventDefault();
      requestWeather();
    });

    if (consumeLocationQuery()) {
      requestWeather();
    }
  }

  window.currentAIWeather = { init: init };
})(window);
