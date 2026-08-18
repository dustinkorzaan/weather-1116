(function (window) {
  'use strict';

  var COMPASS_POINTS = [
    'N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE',
    'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW',
  ];

  var WIND_DIRECTION_ARROW = '\u2B9B';

  /** Expects source degrees already normalized to 0–360 by the API mapper. */
  function degreesToCompass(degrees) {
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return '';
    }
    var index = Math.round(numeric / 22.5) % 16;
    return COMPASS_POINTS[index];
  }

  function windArrowRotationDeg(sourceDegrees) {
    var numeric = Number(sourceDegrees);
    return Number.isFinite(numeric) ? numeric : null;
  }

  function formatWindDirectionWithCompass(compass, degrees) {
    var label = String(compass || '').trim();
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return label;
    }
    var withDegrees = '(' + Math.round(numeric) + '\u00B0)';
    return label ? label + ' ' + withDegrees : withDegrees;
  }

  function formatWindDirectionFromDegrees(degrees) {
    var compass = degreesToCompass(degrees);
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return compass;
    }
    var withDegrees = '(' + Math.round(numeric) + '\u00B0)';
    return compass ? compass + ' ' + withDegrees : withDegrees;
  }

  function renderWindDirection(el, compass, degrees) {
    if (!el) {
      return;
    }

    el.replaceChildren();
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, degrees);
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

  function createWindDirectionCell(degrees) {
    var wrap = document.createElement('span');
    wrap.className = 'wind-direction';
    var label = document.createElement('span');
    label.textContent = formatWindDirectionFromDegrees(degrees);
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

  window.windDirectionDisplay = {
    WIND_DIRECTION_ARROW: WIND_DIRECTION_ARROW,
    windArrowRotationDeg: windArrowRotationDeg,
    degreesToCompass: degreesToCompass,
    formatWindDirectionWithCompass: formatWindDirectionWithCompass,
    formatWindDirectionFromDegrees: formatWindDirectionFromDegrees,
    renderWindDirection: renderWindDirection,
    createWindDirectionCell: createWindDirectionCell,
  };
})(window);
