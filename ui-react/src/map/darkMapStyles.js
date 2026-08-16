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
  };
}

export function createPinIcon(maps, pinFill) {
  return {
    path: maps.SymbolPath.CIRCLE,
    scale: 6,
    fillColor: pinFill,
    fillOpacity: 1,
    strokeWeight: 0,
    labelOrigin: new maps.Point(18, 0),
  };
}

export function applyMapAppearance(maps, map, markers, resolvedTheme) {
  const appearance = getMapAppearance(resolvedTheme);
  map.setOptions({
    styles: appearance.styles,
    backgroundColor: appearance.backgroundColor,
  });
  const icon = createPinIcon(maps, appearance.pinFill);
  markers.forEach((marker) => {
    const label = typeof marker.getLabel === 'function' ? marker.getLabel() : null;
    marker.setIcon(icon);
    if (label) {
      marker.setLabel({
        ...label,
        color: appearance.labelColor,
      });
    }
  });
  return appearance;
}
