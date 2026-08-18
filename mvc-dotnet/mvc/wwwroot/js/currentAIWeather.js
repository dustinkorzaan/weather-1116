(function (window) {
  'use strict';

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

  function formatHemisphereDegrees(value, positiveLabel, negativeLabel) {
    var numeric = Number(value);
    var hemisphere = numeric >= 0 ? positiveLabel : negativeLabel;
    return Math.abs(numeric).toFixed(2) + '\u00B0 ' + hemisphere;
  }

  function formatLatLong(lat, lng) {
    if (!Number.isFinite(Number(lat)) || !Number.isFinite(Number(lng))) {
      return '';
    }
    return formatHemisphereDegrees(lat, 'N', 'S') + ', ' + formatHemisphereDegrees(lng, 'E', 'W');
  }

  function formatTemperatureF(value) {
    var numeric = Number(value);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round(numeric * 10) / 10) + ' \u00B0F';
  }

  function formatWindSpeedMph(value) {
    var numeric = Number(value);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round(numeric * 10) / 10) + ' mph';
  }

  function formatWindDirection(compass, degrees) {
    var label = String(compass || '').trim();
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return label;
    }
    var withDegrees = '(' + Math.round(numeric) + '\u00B0)';
    return label ? label + ' ' + withDegrees : withDegrees;
  }

  var WIND_DIRECTION_ARROW = '\u2B99';

  function windArrowRotationDeg(sourceDegrees) {
    var numeric = Number(sourceDegrees);
    if (!Number.isFinite(numeric)) {
      return null;
    }
    return Math.round(((numeric + 180) % 360 + 360) % 360);
  }

  function renderWindDirection(el, compass, degrees) {
    if (!el) {
      return;
    }

    el.replaceChildren();
    var label = document.createElement('span');
    label.textContent = formatWindDirection(compass, degrees);
    el.appendChild(label);

    var rotation = windArrowRotationDeg(degrees);
    if (rotation !== null) {
      var arrow = document.createElement('span');
      arrow.className = 'wind-direction-arrow';
      arrow.setAttribute('aria-hidden', 'true');
      arrow.textContent = WIND_DIRECTION_ARROW;
      arrow.style.transform = 'rotate(' + rotation + 'deg)';
      el.appendChild(arrow);
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
    var latLongEl = document.getElementById(config.latLongId);

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
          if (window.safeGfmMarkdown) {
            summaryEl.innerHTML = window.safeGfmMarkdown.render(data.fullSummary || '');
          } else {
            summaryEl.textContent = data.fullSummary || '';
          }
          temperatureEl.textContent = formatTemperatureF(data.temperatureF);
          windSpeedEl.textContent = formatWindSpeedMph(data.windSpeedMPH);
          renderWindDirection(windDirectionEl, data.windDirection, data.windDirectionSourceDegrees);
          conditionsEl.textContent = data.conditions || '';
          if (latLongEl) {
            latLongEl.textContent = formatLatLong(data.latitude, data.longitude);
          }
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
      var fromQuery = (params.get('location') || '').trim();
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
