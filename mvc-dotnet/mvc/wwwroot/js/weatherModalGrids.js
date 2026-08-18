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

  /** Formats an Open-Meteo daily date or hourly timestamp as "Wed, Aug 19". */
  function formatCalendarDate(isoDate) {
    var value = String(isoDate || '');
    var date = new Date(value.indexOf('T') >= 0 ? value : value + 'T00:00:00');
    if (Number.isNaN(date.getTime())) {
      return isoDate || '';
    }
    return date.toLocaleDateString(undefined, { weekday: 'short', month: 'short', day: 'numeric' });
  }

  /** Formats an Open-Meteo hourly/15-minute timestamp as "2 PM" (minutes shown only when non-zero). */
  function formatClockTime(isoDateTime) {
    var date = new Date(isoDateTime);
    if (Number.isNaN(date.getTime())) {
      return isoDateTime || '';
    }
    var timeOptions = { hour: 'numeric' };
    if (date.getMinutes() !== 0) {
      timeOptions.minute = '2-digit';
    }
    return date.toLocaleTimeString(undefined, timeOptions);
  }

  /** Formats an already-converted °F value (the API returns US customary units). */
  function formatTemperatureF(fahrenheit) {
    var numeric = Number(fahrenheit);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round(numeric * 10) / 10) + ' °F';
  }

  /** Formats an already-converted mph value (the API returns US customary units). */
  function formatWindSpeedMph(mph) {
    var numeric = Number(mph);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    return (Math.round(numeric * 10) / 10) + ' mph';
  }

  /** Reduces a sixteenths-of-an-inch numerator to lowest terms (denominator is always a power of two). */
  function reduceSixteenths(numerator) {
    var denominator = 16;
    while (numerator !== 0 && numerator % 2 === 0 && denominator > 1) {
      numerator /= 2;
      denominator /= 2;
    }
    return [numerator, denominator];
  }

  /** Formats an already-converted inches value (the API returns US customary units) rounded to the nearest 1/16", e.g. "1 1/2"". */
  function formatPrecipitationIn(inches) {
    var numeric = Number(inches);
    if (!Number.isFinite(numeric)) {
      return '';
    }

    var sixteenths = Math.round(numeric * 16);
    var whole = Math.floor(sixteenths / 16);
    var remainder = sixteenths % 16;

    if (remainder === 0) {
      return whole + '"';
    }

    var reduced = reduceSixteenths(remainder);
    return whole === 0 ? (reduced[0] + '/' + reduced[1] + '"') : (whole + ' ' + reduced[0] + '/' + reduced[1] + '"');
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

  var WIND_DIRECTION_ARROW = '\u27A4';

  function windArrowRotationDeg(degrees) {
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return null;
    }
    return Math.round(numeric) - 90;
  }

  function createWindDirectionCell(degrees) {
    var wrap = document.createElement('span');
    wrap.className = 'wind-direction';
    var label = document.createElement('span');
    label.textContent = formatWindDirection(degrees);
    wrap.appendChild(label);

    var rotation = windArrowRotationDeg(degrees);
    if (rotation !== null) {
      var arrow = document.createElement('span');
      arrow.className = 'wind-direction-arrow';
      arrow.setAttribute('aria-hidden', 'true');
      arrow.textContent = WIND_DIRECTION_ARROW;
      arrow.style.transform = 'rotate(' + rotation + 'deg)';
      wrap.appendChild(arrow);
    }
    return wrap;
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
        formatTemperatureF(daily.temperatureHighF[index]),
        formatTemperatureF(daily.temperatureLowF[index]),
        formatPrecipitationIn(daily.precipitationInch[index]),
        formatWindSpeedMph(daily.windSpeedMPH[index]),
        createWindDirectionCell(daily.windDirectionDegrees[index]),
      ];
    });
  }

  /** Date | Time | Temp | Precip | Wind Speed | Wind Direction — used by the hourly and every-15 tabs. */
  function subDailyRows(series) {
    if (!series || !series.time) {
      return [];
    }
    return series.time.map(function (time, index) {
      return [
        formatCalendarDate(time),
        formatClockTime(time),
        formatTemperatureF(series.temperatureF[index]),
        formatPrecipitationIn(series.precipitationInch[index]),
        formatWindSpeedMph(series.windSpeedMPH[index]),
        createWindDirectionCell(series.windDirectionDegrees[index]),
      ];
    });
  }

  function renderRows(tbody, rows) {
    tbody.replaceChildren();
    rows.forEach(function (cells) {
      var tr = document.createElement('tr');
      cells.forEach(function (cell) {
        var td = document.createElement('td');
        if (cell instanceof Node) {
          td.appendChild(cell);
        } else {
          td.textContent = cell;
        }
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
   * @param {'daily'|'hourly'|'minutely15'} config.field which response field holds the series
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
