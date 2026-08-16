import { expect, test, vi } from 'vitest';
import {
  AERIAL_VIEW_LABEL,
  AERIAL_VIEW_TYPE,
  MAP_TYPE_TOGGLE_LABEL,
  MAP_VIEW_LABEL,
  MAP_VIEW_TYPE,
  bindMapTypeToggle,
  resolveMapTypeId,
  viewTypeFromMapTypeId,
} from './mapTypeToggle';

const maps = {
  MapTypeId: { ROADMAP: 'roadmap', HYBRID: 'hybrid', SATELLITE: 'satellite' },
  ControlPosition: { TOP_LEFT: 1 },
};

function createMap() {
  const controls = {
    1: { push: vi.fn() },
  };
  return {
    setMapTypeId: vi.fn(),
    controls,
  };
}

test('resolveMapTypeId maps Map to roadmap and Aerial to hybrid', () => {
  expect(resolveMapTypeId(maps, MAP_VIEW_TYPE)).toBe('roadmap');
  expect(resolveMapTypeId(maps, AERIAL_VIEW_TYPE)).toBe('hybrid');
  expect(resolveMapTypeId({}, AERIAL_VIEW_TYPE)).toBe('hybrid');
  expect(resolveMapTypeId({}, MAP_VIEW_TYPE)).toBe('roadmap');
});

test('viewTypeFromMapTypeId treats satellite and hybrid as aerial', () => {
  expect(viewTypeFromMapTypeId('roadmap')).toBe(MAP_VIEW_TYPE);
  expect(viewTypeFromMapTypeId('hybrid')).toBe(AERIAL_VIEW_TYPE);
  expect(viewTypeFromMapTypeId('satellite')).toBe(AERIAL_VIEW_TYPE);
  expect(viewTypeFromMapTypeId('HYBRID')).toBe(AERIAL_VIEW_TYPE);
  expect(viewTypeFromMapTypeId(undefined)).toBe(MAP_VIEW_TYPE);
});

test('toggle control switches the map between Map and Aerial', () => {
  const map = createMap();
  const onChange = vi.fn();

  const control = bindMapTypeToggle({
    maps,
    map,
    initialViewType: MAP_VIEW_TYPE,
    onChange,
  });

  expect(control.element.getAttribute('role')).toBe('group');
  expect(control.element.getAttribute('aria-label')).toBe(MAP_TYPE_TOGGLE_LABEL);
  expect(map.controls[1].push).toHaveBeenCalledWith(control.element);
  expect(map.setMapTypeId).toHaveBeenCalledWith('roadmap');

  const [mapButton, aerialButton] = control.element.querySelectorAll('.weather-map-type-toggle-button');
  expect(mapButton.textContent).toBe(MAP_VIEW_LABEL);
  expect(aerialButton.textContent).toBe(AERIAL_VIEW_LABEL);
  expect(mapButton.getAttribute('aria-pressed')).toBe('true');
  expect(aerialButton.getAttribute('aria-pressed')).toBe('false');

  aerialButton.click();
  expect(map.setMapTypeId).toHaveBeenCalledWith('hybrid');
  expect(aerialButton.getAttribute('aria-pressed')).toBe('true');
  expect(mapButton.getAttribute('aria-pressed')).toBe('false');
  expect(onChange).toHaveBeenLastCalledWith(AERIAL_VIEW_TYPE);

  mapButton.click();
  expect(map.setMapTypeId).toHaveBeenLastCalledWith('roadmap');
  expect(onChange).toHaveBeenLastCalledWith(MAP_VIEW_TYPE);
});

test('initial aerial view selects Aerial and sets hybrid', () => {
  const map = createMap();

  const control = bindMapTypeToggle({
    maps,
    map,
    initialViewType: AERIAL_VIEW_TYPE,
  });

  const [mapButton, aerialButton] = control.element.querySelectorAll('.weather-map-type-toggle-button');
  expect(map.setMapTypeId).toHaveBeenCalledWith('hybrid');
  expect(mapButton.getAttribute('aria-pressed')).toBe('false');
  expect(aerialButton.getAttribute('aria-pressed')).toBe('true');
});
