(function (window) {
  'use strict';

  var WIND_DIRECTION_ARROW = 'V';

  function normalizeSourceDegrees(deg) {
    var numeric = Number(deg);
    if (!Number.isFinite(numeric)) {
      return 0;
    }
    return Math.round(((numeric % 360) + 360) % 360);
  }

  function formatWindDirectionWithCompass(compass, degrees) {
    var label = String(compass || '').trim();
    var withDegrees = '(' + normalizeSourceDegrees(degrees) + '\u00B0)';
    return label ? label + ' ' + withDegrees : withDegrees;
  }

  function renderWindDirection(el, compass, degrees) {
    if (!el) {
      return;
    }

    var rotationDeg = normalizeSourceDegrees(degrees);
    el.replaceChildren();
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, rotationDeg);
    el.appendChild(label);

    var arrow = document.createElement('span');
    arrow.className = 'wind-direction-arrow';
    arrow.setAttribute('aria-hidden', 'true');
    arrow.textContent = WIND_DIRECTION_ARROW;
    arrow.style.transform = 'rotate(' + rotationDeg + 'deg)';
    el.appendChild(arrow);
  }

  function createWindDirectionCell(compass, degrees) {
    var rotationDeg = normalizeSourceDegrees(degrees);
    var wrap = document.createElement('span');
    wrap.className = 'wind-direction';
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, rotationDeg);
    wrap.appendChild(label);

    var arrow = document.createElement('span');
    arrow.className = 'wind-direction-arrow';
    arrow.setAttribute('aria-hidden', 'true');
    arrow.textContent = WIND_DIRECTION_ARROW;
    arrow.style.transform = 'rotate(' + rotationDeg + 'deg)';
    wrap.appendChild(arrow);
    return wrap;
  }

  window.windDirectionDisplay = {
    WIND_DIRECTION_ARROW: WIND_DIRECTION_ARROW,
    normalizeSourceDegrees: normalizeSourceDegrees,
    formatWindDirectionWithCompass: formatWindDirectionWithCompass,
    renderWindDirection: renderWindDirection,
    createWindDirectionCell: createWindDirectionCell,
  };
})(window);
