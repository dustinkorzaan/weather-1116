import { logoPinUrl } from './logoPinOverlay';

export const DARK_MAP_BACKGROUND = '#0b111d';
export const LIGHT_MAP_BACKGROUND = '#e8eef4';

/** Light canvas Google Maps styles for the site light theme. */
export const LIGHT_MAP_STYLES = [
  { elementType: 'geometry', stylers: [{ color: '#e8eef4' }] },
  { elementType: 'labels.text.fill', stylers: [{ color: '#4b5563' }] },
  { elementType: 'labels.text.stroke', stylers: [{ color: '#e8eef4' }] },
  {
    featureType: 'administrative',
    elementType: 'geometry.stroke',
    stylers: [{ color: '#cbd5e1' }],
  },
  {
    featureType: 'administrative.land_parcel',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'administrative.neighborhood',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'administrative.province',
    elementType: 'labels.text.fill',
    stylers: [{ color: '#4b5563' }],
  },
  {
    featureType: 'poi',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'road',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'transit',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'water',
    elementType: 'geometry',
    stylers: [{ color: '#c5d4e0' }],
  },
  {
    featureType: 'water',
    elementType: 'labels.text.fill',
    stylers: [{ color: '#64748b' }],
  },
];

/** Dark navy Google Maps styles matching the weather map mockup. */
export const DARK_MAP_STYLES = [
  { elementType: 'geometry', stylers: [{ color: '#0b111d' }] },
  { elementType: 'labels.text.fill', stylers: [{ color: '#a1a1aa' }] },
  { elementType: 'labels.text.stroke', stylers: [{ color: '#0b111d' }] },
  {
    featureType: 'administrative',
    elementType: 'geometry.stroke',
    stylers: [{ color: '#3f3f46' }],
  },
  {
    featureType: 'administrative.land_parcel',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'administrative.neighborhood',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'administrative.province',
    elementType: 'labels.text.fill',
    stylers: [{ color: '#a1a1aa' }],
  },
  {
    featureType: 'poi',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'road',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'transit',
    stylers: [{ visibility: 'off' }],
  },
  {
    featureType: 'water',
    elementType: 'geometry',
    stylers: [{ color: '#060a12' }],
  },
  {
    featureType: 'water',
    elementType: 'labels.text.fill',
    stylers: [{ color: '#52525b' }],
  },
];

export function getMapAppearance(resolvedTheme) {
  const isDark = resolvedTheme === 'dark';
  return {
    styles: isDark ? DARK_MAP_STYLES : LIGHT_MAP_STYLES,
    backgroundColor: isDark ? DARK_MAP_BACKGROUND : LIGHT_MAP_BACKGROUND,
    pinFill: isDark ? '#ffffff' : '#111827',
    labelColor: isDark ? '#e4e4e7' : '#1f2937',
    colorScheme: isDark ? 'DARK' : 'LIGHT',
  };
}

export function mapColorScheme(maps, resolvedTheme) {
  const isDark = resolvedTheme === 'dark';
  const schemes = maps?.ColorScheme;
  if (schemes) {
    return isDark ? schemes.DARK : schemes.LIGHT;
  }
  return isDark ? 'DARK' : 'LIGHT';
}

export function mapRenderingType(maps) {
  return maps?.RenderingType?.RASTER ?? 'RASTER';
}

export function applyMapColorSchemeCss(element, resolvedTheme) {
  if (!element || !element.style) {
    return;
  }
  element.style.colorScheme = resolvedTheme === 'dark' ? 'dark' : 'light';
}

/**
 * Google Maps ignores JSON `styles` on vector maps, and `colorScheme` is
 * init-only. Raster + an explicit LIGHT/DARK scheme keeps the canvas in
 * sync with the site theme (not the OS preference).
 */
export function createMapOptions(maps, resolvedTheme, extras = {}) {
  const appearance = getMapAppearance(resolvedTheme);
  return {
    center: extras.center,
    zoom: extras.zoom,
    mapTypeId: extras.mapTypeId,
    styles: appearance.styles,
    disableDefaultUI: true,
    zoomControl: true,
    mapTypeControl: false,
    streetViewControl: false,
    fullscreenControl: false,
    backgroundColor: appearance.backgroundColor,
    colorScheme: mapColorScheme(maps, resolvedTheme),
    renderingType: mapRenderingType(maps),
  };
}

export function applyMapAppearance(maps, map, pins, resolvedTheme) {
  const appearance = getMapAppearance(resolvedTheme);
  if (typeof map.getDiv === 'function') {
    applyMapColorSchemeCss(map.getDiv(), resolvedTheme);
  }
  map.setOptions({
    styles: appearance.styles,
    backgroundColor: appearance.backgroundColor,
    colorScheme: mapColorScheme(maps, resolvedTheme),
  });
  const url = logoPinUrl(resolvedTheme);
  pins?.forEach((pin) => {
    if (typeof pin.setLogoUrl === 'function') {
      pin.setLogoUrl(url);
    }
  });
  return appearance;
}
