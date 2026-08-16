export const LOGO_PIN_SPIN_DURATION_SEC = 16;
export const LOGO_PIN_DARK_URL = '/logo-solid.svg';
export const LOGO_PIN_LIGHT_URL = '/logo-black-solid.svg';

const MARKER_TO_DOM_EVENT = {
  mouseover: 'mouseenter',
  mouseout: 'mouseleave',
  click: 'click',
};

/**
 * Solid site logo used as the map pin. Dark maps use the filled gray mark;
 * light maps use the filled black mark. Header logos stay the outline versions.
 * @param {string} resolvedTheme
 * @returns {string}
 */
export function logoPinUrl(resolvedTheme) {
  return resolvedTheme === 'dark' ? LOGO_PIN_DARK_URL : LOGO_PIN_LIGHT_URL;
}

/**
 * Stagger spin start times so neighboring pins are not in lockstep.
 * @param {number} index
 * @returns {number}
 */
export function logoPinSpinOffsetSec(index) {
  return ((Number(index) || 0) * 3.7) % LOGO_PIN_SPIN_DURATION_SEC;
}

function bindDomEvent(element, mapsEventName, handler) {
  const domEvent = MARKER_TO_DOM_EVENT[mapsEventName] || mapsEventName;
  element.addEventListener(domEvent, handler);
}

function stopMapClick(event) {
  event.stopPropagation();
}

/**
 * HTML overlay pin: the weather logo, slowly spinning until hover.
 * Mimics enough of a Marker for InfoWindow anchoring (`getPosition`, `addListener`).
 * @param {{
 *   OverlayView: new () => object,
 *   LatLng: new (lat: number, lng: number) => object,
 * }} maps
 * @param {{
 *   lat: number,
 *   lng: number,
 *   cityName: string,
 *   logoUrl: string,
 *   spinOffsetSec?: number,
 * }} options
 */
export function createLogoPinOverlay(maps, options) {
  class LogoPinOverlay extends maps.OverlayView {
    constructor() {
      super();
      this.position = new maps.LatLng(options.lat, options.lng);
      if (typeof this.set === 'function') {
        this.set('position', this.position);
      }
      this.cityName = options.cityName;
      this.logoUrl = options.logoUrl;
      this.spinOffsetSec = options.spinOffsetSec ?? 0;
      this.pendingListeners = [];
      this.div = null;
    }

    onAdd() {
      const pin = document.createElement('button');
      pin.type = 'button';
      pin.className = 'weather-map-logo-pin';
      pin.setAttribute('aria-label', this.cityName);

      const pulse = document.createElement('span');
      pulse.className = 'weather-map-logo-pin-pulse';
      pulse.setAttribute('aria-hidden', 'true');

      const spinner = document.createElement('span');
      spinner.className = 'weather-map-logo-pin-spin';
      spinner.style.animationDelay = `-${this.spinOffsetSec}s`;

      const image = document.createElement('img');
      image.className = 'weather-map-logo-pin-image';
      image.src = this.logoUrl;
      image.alt = '';
      image.draggable = false;

      spinner.appendChild(image);
      pin.append(pulse, spinner);
      pin.addEventListener('click', stopMapClick);
      pin.addEventListener('mousedown', stopMapClick);

      this.div = pin;
      const panes = this.getPanes();
      if (!panes?.overlayMouseTarget) {
        return;
      }
      panes.overlayMouseTarget.appendChild(pin);
      this.pendingListeners.forEach(({ eventName, handler }) => {
        bindDomEvent(pin, eventName, handler);
      });
      this.pendingListeners = [];
    }

    draw() {
      const projection = this.getProjection();
      if (!projection || !this.div) {
        return;
      }

      const point = projection.fromLatLngToDivPixel(this.position);
      if (!point) {
        return;
      }

      this.div.style.left = `${point.x}px`;
      this.div.style.top = `${point.y}px`;
    }

    onRemove() {
      this.div?.remove();
      this.div = null;
    }

    getPosition() {
      return this.position;
    }

    addListener(eventName, handler) {
      if (this.div) {
        bindDomEvent(this.div, eventName, handler);
        return;
      }

      this.pendingListeners.push({ eventName, handler });
    }

    setPaused(paused) {
      this.div?.classList.toggle('is-paused', Boolean(paused));
    }

    setLogoUrl(url) {
      this.logoUrl = url;
      const image = this.div?.querySelector('.weather-map-logo-pin-image');
      if (image) {
        image.src = url;
      }
    }
  }

  return new LogoPinOverlay();
}
