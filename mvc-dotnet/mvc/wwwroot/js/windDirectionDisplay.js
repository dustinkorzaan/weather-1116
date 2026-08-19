(function (window) {
  'use strict';

  var SVG_NS = 'http://www.w3.org/2000/svg';
  // Down-pointing filled arrowhead. SVG instead of U+2B9B, which many mobile fonts lack.
  // At 0° the arrow points south (wind from north).
  var WIND_DIRECTION_ARROW_PATH = 'M6 11 1.2 2.5h9.6Z';

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

  function createWindDirectionArrow(rotationDeg) {
    var svg = document.createElementNS(SVG_NS, 'svg');
    svg.setAttribute('class', 'wind-direction-arrow');
    svg.setAttribute('aria-hidden', 'true');
    svg.setAttribute('viewBox', '0 0 12 12');
    svg.setAttribute('width', '1.15em');
    svg.setAttribute('height', '1.15em');
    svg.style.transform = 'rotate(' + rotationDeg + 'deg)';
    var path = document.createElementNS(SVG_NS, 'path');
    path.setAttribute('fill', 'currentColor');
    path.setAttribute('d', WIND_DIRECTION_ARROW_PATH);
    svg.appendChild(path);
    return svg;
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
    el.appendChild(createWindDirectionArrow(rotationDeg));
  }

  function createWindDirectionCell(compass, degrees) {
    var rotationDeg = normalizeSourceDegrees(degrees);
    var wrap = document.createElement('span');
    wrap.className = 'wind-direction';
    var label = document.createElement('span');
    label.textContent = formatWindDirectionWithCompass(compass, rotationDeg);
    wrap.appendChild(label);
    wrap.appendChild(createWindDirectionArrow(rotationDeg));
    return wrap;
  }

  window.windDirectionDisplay = {
    normalizeSourceDegrees: normalizeSourceDegrees,
    formatWindDirectionWithCompass: formatWindDirectionWithCompass,
    renderWindDirection: renderWindDirection,
    createWindDirectionCell: createWindDirectionCell,
  };
})(window);
