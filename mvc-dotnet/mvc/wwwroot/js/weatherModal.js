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
    return Math.abs(numeric).toFixed(2) + '° ' + hemisphere;
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
    return (Math.round(numeric * 10) / 10) + ' °F';
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
    var withDegrees = '(' + Math.round(numeric) + '°)';
    return label ? label + ' ' + withDegrees : withDegrees;
  }

  var WIND_DIRECTION_ARROW = '➤';

  function windArrowRotationDeg(degrees) {
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return null;
    }
    // ➤ points right at 0° CSS; subtract 90 so 0° (toward north) points up.
    return Math.round(numeric) - 90;
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
    var refreshButton = document.getElementById(config.refreshButtonId);
    var errorEl = document.getElementById(config.errorId);
    var resultsEl = document.getElementById(config.resultsId);
    var summaryEl = document.getElementById(config.summaryId);
    var temperatureEl = document.getElementById(config.temperatureId);
    var windSpeedEl = document.getElementById(config.windSpeedId);
    var windDirectionEl = document.getElementById(config.windDirectionId);
    var conditionsEl = document.getElementById(config.conditionsId);
    var latLongEl = document.getElementById(config.latLongId);

    if (!refreshButton) {
      return;
    }

    function requestWeather() {
      setHidden(errorEl, true);
      setHidden(resultsEl, true);
      refreshButton.disabled = true;
      refreshButton.classList.add('is-spinning');

      var url = config.endpoint + (config.endpoint.indexOf('?') >= 0 ? '&' : '?') +
        'location=' + encodeURIComponent(config.location || '');

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
          renderWindDirection(windDirectionEl, data.windDirection, data.windDirectionToDegrees);
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
          refreshButton.disabled = false;
          refreshButton.classList.remove('is-spinning');
        });
    }

    refreshButton.addEventListener('click', requestWeather);
    requestWeather();
  }

  window.weatherModal = { init: init };
})(window);
