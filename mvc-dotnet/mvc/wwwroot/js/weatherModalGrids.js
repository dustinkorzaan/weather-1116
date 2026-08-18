(function (window) {
  'use strict';

  var COMPASS_POINTS = [
    'N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE',
    'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW',
  ];

  /** Converts meteorological degrees to a 16-point compass abbreviation. */
  function degreesToCompass(degrees) {
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    var index = Math.round((((numeric % 360) + 360) % 360) / 22.5) % 16;
    return COMPASS_POINTS[index];
  }

  /** Formats an Open-Meteo daily date ("2026-08-19") as "Wed, Aug 19". */
  function formatCalendarDate(isoDate) {
    var date = new Date(isoDate + 'T00:00:00');
    if (Number.isNaN(date.getTime())) {
      return isoDate || '';
    }
    return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  }

  /** Formats an Open-Meteo hourly/15-minute timestamp as "Wed, Aug 19, 2 PM" (minutes shown only when non-zero). */
  function formatClockTime(isoDateTime) {
    var date = new Date(isoDateTime);
    if (Number.isNaN(date.getTime())) {
      return isoDateTime || '';
    }
    var datePart = date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
    var timeOptions = { hour: 'numeric' };
    if (date.getMinutes() !== 0) {
      timeOptions.minute = '2-digit';
    }
    var timePart = date.toLocaleTimeString(undefined, timeOptions);
    return datePart + ', ' + timePart;
  }

  /** Converts Open-Meteo °C to °F, then formats. */
  function formatTemperatureF(celsius) {
    var numeric = Number(celsius);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round((numeric * 9 / 5 + 32) * 10) / 10) + ' °F';
  }

  /** Converts Open-Meteo km/h to mph, then formats. */
  function formatWindSpeedMph(kilometersPerHour) {
    var numeric = Number(kilometersPerHour);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round((numeric / 1.609344) * 10) / 10) + ' mph';
  }

  /** Converts Open-Meteo mm to inches, then formats. */
  function formatPrecipitationIn(millimeters) {
    var numeric = Number(millimeters);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round((numeric / 25.4) * 100) / 100) + '"';
  }

  function formatWindDirection(degrees) {
    var compass = degreesToCompass(degrees);
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return compass;
    }
    var withDegrees = '(' + Math.round(numeric) + '°)';
    return compass ? compass + ' ' + withDegrees : withDegrees;
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

  /** Date | High | Low | Precip | Wind Speed | Wind Direction — used by the two daily tabs. */
  function dailyRows(daily) {
    if (!daily || !daily.time) {
      return [];
    }
    return daily.time.map(function (time, index) {
      return [
        formatCalendarDate(time),
        formatTemperatureF(daily.temperature_2m_max[index]),
        formatTemperatureF(daily.temperature_2m_min[index]),
        formatPrecipitationIn(daily.precipitation_sum[index]),
        formatWindSpeedMph(daily.wind_speed_10m_max[index]),
        formatWindDirection(daily.wind_direction_10m_dominant[index]),
      ];
    });
  }

  /** Time | Temp | Precip | Wind Speed | Wind Direction — used by the hourly and every-15 tabs. */
  function subDailyRows(series) {
    if (!series || !series.time) {
      return [];
    }
    return series.time.map(function (time, index) {
      return [
        formatClockTime(time),
        formatTemperatureF(series.temperature_2m[index]),
        formatPrecipitationIn(series.precipitation[index]),
        formatWindSpeedMph(series.wind_speed_10m[index]),
        formatWindDirection(series.wind_direction_10m[index]),
      ];
    });
  }

  function renderRows(tbody, rows) {
    tbody.replaceChildren();
    rows.forEach(function (cells) {
      var tr = document.createElement('tr');
      cells.forEach(function (text) {
        var td = document.createElement('td');
        td.textContent = text;
        tr.appendChild(td);
      });
      tbody.appendChild(tr);
    });
  }

  /**
   * @param {object} config
   * @param {string} config.refreshButtonId
   * @param {string} config.errorId
   * @param {string} config.wrapId
   * @param {string} config.bodyId
   * @param {string} config.endpoint '/Forecast' or '/History'
   * @param {string} config.resolution 'Daily' | 'Hourly' | 'FifteenMinutes'
   * @param {'daily'|'hourly'|'minutely_15'} config.field which response field holds the series
   * @param {boolean} config.reverse most-recent-first (history tabs)
   * @param {number} config.lat
   * @param {number} config.lng
   */
  function init(config) {
    var refreshButton = document.getElementById(config.refreshButtonId);
    var errorEl = document.getElementById(config.errorId);
    var wrapEl = document.getElementById(config.wrapId);
    var bodyEl = document.getElementById(config.bodyId);

    if (!refreshButton || !bodyEl) {
      return;
    }

    function rowsFor(data) {
      var series = data && data[config.field];
      var rows = config.field === 'daily' ? dailyRows(series) : subDailyRows(series);
      if (config.reverse) {
        rows.reverse();
      }
      return rows;
    }

    function load() {
      setHidden(errorEl, true);
      setHidden(wrapEl, true);
      refreshButton.disabled = true;
      refreshButton.classList.add('is-spinning');

      var url = config.endpoint +
        '?latitude=' + encodeURIComponent(config.lat) +
        '&longitude=' + encodeURIComponent(config.lng) +
        '&resolution=' + encodeURIComponent(config.resolution);

      fetch(url, { headers: { Accept: 'application/json' } })
        .then(function (response) {
          if (!response.ok) {
            throw new Error('Request failed');
          }
          return response.json();
        })
        .then(function (data) {
          var rows = rowsFor(data);
          renderRows(bodyEl, rows);
          setHidden(wrapEl, rows.length === 0);
        })
        .catch(function () {
          if (errorEl) {
            errorEl.textContent = config.errorMessage || 'Unable to load data.';
            setHidden(errorEl, false);
          }
        })
        .finally(function () {
          refreshButton.disabled = false;
          refreshButton.classList.remove('is-spinning');
        });
    }

    refreshButton.addEventListener('click', load);
    load();
  }

  window.weatherModalGrids = { init: init };
})(window);
