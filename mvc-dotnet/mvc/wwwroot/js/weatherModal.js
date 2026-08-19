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

  function formatRunLogTimestamp(dateTimeUtc) {
    var date = new Date(dateTimeUtc);
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    return date.toISOString().slice(11, 23);
  }

  function formatRunLogMs(ms) {
    return Number.isFinite(ms) ? Math.round(ms).toLocaleString() : '';
  }

  function renderRunLogRows(tbody, runLogDetails) {
    tbody.replaceChildren();
    runLogDetails.forEach(function (entry) {
      var cells = [
        formatRunLogTimestamp(entry.dateTimeUtc),
        entry.loopNumber,
        entry.message,
        entry.inputTokenCount != null ? entry.inputTokenCount : '',
        entry.cachedTokenCount != null ? entry.cachedTokenCount : '',
        entry.outputTokenCount != null ? entry.outputTokenCount : '',
        entry.reasoningTokenCount != null ? entry.reasoningTokenCount : '',
        entry.totalTokenCount != null ? entry.totalTokenCount : '',
        formatRunLogMs(entry.runtimeMs),
        formatRunLogMs(entry.loopRuntimeMs),
        formatRunLogMs(entry.runningTotalMs),
      ];
      var tr = document.createElement('tr');
      cells.forEach(function (cell) {
        var td = document.createElement('td');
        td.textContent = cell;
        tr.appendChild(td);
      });
      tbody.appendChild(tr);
    });
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
    var runLogWrapEl = document.getElementById(config.runLogWrapId);
    var runLogBodyEl = document.getElementById(config.runLogBodyId);
    var runLogTotalEl = document.getElementById(config.runLogTotalId);

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
          window.windDirectionDisplay.renderWindDirection(
            windDirectionEl,
            data.windDirectionSource,
            data.windDirectionSourceDegrees);
          conditionsEl.textContent = data.conditions || '';
          if (latLongEl) {
            latLongEl.textContent = formatLatLong(data.latitude, data.longitude);
          }
          var runLogDetails = data.runLogDetails || [];
          if (runLogWrapEl && runLogBodyEl) {
            if (runLogDetails.length > 0) {
              renderRunLogRows(runLogBodyEl, runLogDetails);
              if (runLogTotalEl) {
                runLogTotalEl.textContent = 'Total Runtime: ' +
                  formatRunLogMs(runLogDetails[runLogDetails.length - 1].runningTotalMs) + ' ms';
              }
              setHidden(runLogWrapEl, false);
            } else {
              setHidden(runLogWrapEl, true);
            }
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
