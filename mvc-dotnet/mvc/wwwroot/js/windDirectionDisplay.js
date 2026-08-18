(function (window) {
  'use strict';

  var WIND_DIRECTION_ARROW = '\u2B9B';

  function formatWindDirectionWithCompass(compass, degrees) {
    var label = String(compass || '').trim();
    var numeric = Number(degrees);
    if (!Number.isFinite(numeric)) {
      return label;
    }
    var withDegrees = '(' + Math.round(numeric) + '\u00B0)';
    return label ? label + ' ' + withDegrees : withDegrees;
  }

  function renderWindDirection(el, compass, degrees) {
    if (!el) {
      return;
    }

    el.replaceChildren();
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, degrees);
    el.appendChild(label);

    var arrow = document.createElement('span');
    arrow.className = 'wind-direction-arrow';
    arrow.setAttribute('aria-hidden', 'true');
    arrow.textContent = WIND_DIRECTION_ARROW;
    arrow.style.transform = 'rotate(' + degrees + 'deg)';
    el.appendChild(arrow);
  }

  function createWindDirectionCell(compass, degrees) {
    var wrap = document.createElement('span');
    wrap.className = 'wind-direction';
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, degrees);
    wrap.appendChild(label);

    var arrow = document.createElement('span');
    arrow.className = 'wind-direction-arrow';
    arrow.setAttribute('aria-hidden', 'true');
    arrow.textContent = WIND_DIRECTION_ARROW;
    arrow.style.transform = 'rotate(' + degrees + 'deg)';
    wrap.appendChild(arrow);
    return wrap;
  }

  window.windDirectionDisplay = {
    WIND_DIRECTION_ARROW: WIND_DIRECTION_ARROW,
    formatWindDirectionWithCompass: formatWindDirectionWithCompass,
    renderWindDirection: renderWindDirection,
    createWindDirectionCell: createWindDirectionCell,
  };
})(window);
