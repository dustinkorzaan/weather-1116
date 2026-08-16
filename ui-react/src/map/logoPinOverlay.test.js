import { expect, test, vi } from 'vitest';
import {
  LOGO_PIN_DARK_URL,
  LOGO_PIN_LIGHT_URL,
  LOGO_PIN_SPIN_DURATION_SEC,
  createLogoPinOverlay,
  logoPinSpinOffsetSec,
  logoPinUrl,
} from './logoPinOverlay';

class FakeOverlayView {
  constructor() {
    this._map = null;
  }

  setMap(map) {
    this._map = map;
    if (map) {
      this.onAdd();
      this.draw();
    } else {
      this.onRemove();
    }
  }

  getPanes() {
    return { overlayMouseTarget: this._pane };
  }

  getProjection() {
    return {
      fromLatLngToDivPixel() {
        return { x: 40, y: 80 };
      },
    };
  }
}

function LatLng(lat, lng) {
  this.lat = () => lat;
  this.lng = () => lng;
}

function createMaps(pane) {
  class OverlayView extends FakeOverlayView {
    constructor() {
      super();
      this._pane = pane;
    }
  }

  return { OverlayView, LatLng };
}

test('dark theme uses the gray logo and light theme uses the black logo', () => {
  expect(logoPinUrl('dark')).toBe(LOGO_PIN_DARK_URL);
  expect(logoPinUrl('light')).toBe(LOGO_PIN_LIGHT_URL);
});

test('spin offsets stagger within one revolution', () => {
  expect(logoPinSpinOffsetSec(0)).toBe(0);
  expect(logoPinSpinOffsetSec(1)).toBe(3.7);
  expect(logoPinSpinOffsetSec(5)).toBeLessThan(LOGO_PIN_SPIN_DURATION_SEC);
});

test('logo overlay renders a spinning image without a city-name label', () => {
  const pane = document.createElement('div');
  const overlay = createLogoPinOverlay(createMaps(pane), {
    lat: 33.749,
    lng: -84.388,
    cityName: 'Atlanta, GA',
    logoUrl: LOGO_PIN_DARK_URL,
    spinOffsetSec: 3.7,
  });

  overlay.setMap({});

  const pin = pane.querySelector('.weather-map-logo-pin');
  const image = pane.querySelector('.weather-map-logo-pin-image');
  const spinner = pane.querySelector('.weather-map-logo-pin-spin');

  expect(pin?.getAttribute('aria-label')).toBe('Atlanta, GA');
  expect(image?.src).toContain(LOGO_PIN_DARK_URL);
  expect(spinner?.style.animationDelay).toBe('-3.7s');
  expect(pin?.textContent).toBe('');
  expect(pin.style.left).toBe('40px');
  expect(pin.style.top).toBe('80px');
});

test('hover pause class and theme logo updates apply to the overlay', () => {
  const pane = document.createElement('div');
  const overlay = createLogoPinOverlay(createMaps(pane), {
    lat: 40.7128,
    lng: -74.006,
    cityName: 'New York, NY',
    logoUrl: LOGO_PIN_LIGHT_URL,
  });

  overlay.setMap({});
  overlay.setPaused(true);
  overlay.setLogoUrl(LOGO_PIN_DARK_URL);

  const pin = pane.querySelector('.weather-map-logo-pin');
  expect(pin?.classList.contains('is-paused')).toBe(true);
  expect(pane.querySelector('.weather-map-logo-pin-image')?.src).toContain(
    LOGO_PIN_DARK_URL
  );

  overlay.setMap(null);
  expect(pane.querySelector('.weather-map-logo-pin')).toBeNull();
});

test('queued marker listeners bind to the pin once it is on the map', () => {
  const pane = document.createElement('div');
  const overlay = createLogoPinOverlay(createMaps(pane), {
    lat: 43.6532,
    lng: -79.3832,
    cityName: 'Toronto, ON',
    logoUrl: LOGO_PIN_LIGHT_URL,
  });
  const onHover = vi.fn();

  overlay.addListener('mouseover', onHover);
  overlay.setMap({});
  pane.querySelector('.weather-map-logo-pin')?.dispatchEvent(new Event('mouseenter'));

  expect(onHover).toHaveBeenCalledTimes(1);
  expect(overlay.getPosition().lat()).toBe(43.6532);
});
