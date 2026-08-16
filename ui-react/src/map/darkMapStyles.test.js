import { afterEach, expect, test, vi } from 'vitest';
import {
  DARK_MAP_BACKGROUND,
  DARK_MAP_STYLES,
  LIGHT_MAP_BACKGROUND,
  LIGHT_MAP_STYLES,
  applyMapAppearance,
  applyMapColorSchemeCss,
  createMapOptions,
  getMapAppearance,
  mapColorScheme,
  mapRenderingType,
} from './darkMapStyles';
import { LOGO_PIN_DARK_URL, LOGO_PIN_LIGHT_URL } from './logoPinOverlay';

afterEach(() => {
  document.documentElement.style.colorScheme = '';
});

test('light site theme uses the light map canvas, pins, and color scheme', () => {
  const appearance = getMapAppearance('light');

  expect(appearance.styles).toBe(LIGHT_MAP_STYLES);
  expect(appearance.backgroundColor).toBe(LIGHT_MAP_BACKGROUND);
  expect(appearance.pinFill).toBe('#111827');
  expect(appearance.labelColor).toBe('#1f2937');
  expect(appearance.colorScheme).toBe('LIGHT');
});

test('dark site theme uses the dark map canvas, pins, and color scheme', () => {
  const appearance = getMapAppearance('dark');

  expect(appearance.styles).toBe(DARK_MAP_STYLES);
  expect(appearance.backgroundColor).toBe(DARK_MAP_BACKGROUND);
  expect(appearance.pinFill).toBe('#ffffff');
  expect(appearance.labelColor).toBe('#e4e4e7');
  expect(appearance.colorScheme).toBe('DARK');
});

test('createMapOptions forces raster rendering and LIGHT colorScheme for the light theme', () => {
  const maps = {
    ColorScheme: { LIGHT: 'LIGHT', DARK: 'DARK' },
    RenderingType: { RASTER: 'RASTER', VECTOR: 'VECTOR' },
    MapTypeId: { ROADMAP: 'roadmap', SATELLITE: 'satellite', HYBRID: 'hybrid', TERRAIN: 'terrain' },
    MapTypeControlStyle: { HORIZONTAL_BAR: 'HORIZONTAL_BAR' },
    ControlPosition: { TOP_LEFT: 'TOP_LEFT' },
  };

  const options = createMapOptions(maps, 'light', {
    center: { lat: 1, lng: 2 },
    zoom: 4,
  });

  expect(options.colorScheme).toBe('LIGHT');
  expect(options.renderingType).toBe('RASTER');
  expect(options.styles).toBe(LIGHT_MAP_STYLES);
  expect(options.backgroundColor).toBe(LIGHT_MAP_BACKGROUND);
  expect(options.center).toEqual({ lat: 1, lng: 2 });
  expect(options.zoom).toBe(4);
  expect(options.mapTypeId).toBe('hybrid');
  expect(options.mapTypeControl).toBe(true);
  expect(options.mapTypeControlOptions).toEqual({
    style: 'HORIZONTAL_BAR',
    position: 'TOP_LEFT',
    mapTypeIds: ['roadmap', 'satellite', 'hybrid', 'terrain'],
  });
});

test('mapColorScheme and mapRenderingType fall back to strings without the Maps enums', () => {
  expect(mapColorScheme({}, 'light')).toBe('LIGHT');
  expect(mapColorScheme({}, 'dark')).toBe('DARK');
  expect(mapRenderingType({})).toBe('RASTER');
});

test('applyMapColorSchemeCss follows the resolved site theme, not the OS', () => {
  const element = document.createElement('div');

  applyMapColorSchemeCss(element, 'light');
  expect(element.style.colorScheme).toBe('light');

  applyMapColorSchemeCss(element, 'dark');
  expect(element.style.colorScheme).toBe('dark');
});

test('applyMapAppearance swaps logo pin artwork with the site theme', () => {
  const map = {
    setOptions: vi.fn(),
  };
  const pin = {
    setLogoUrl: vi.fn(),
  };

  applyMapAppearance({}, map, [pin], 'dark');
  expect(pin.setLogoUrl).toHaveBeenCalledWith(LOGO_PIN_DARK_URL);

  applyMapAppearance({}, map, [pin], 'light');
  expect(pin.setLogoUrl).toHaveBeenCalledWith(LOGO_PIN_LIGHT_URL);
});
