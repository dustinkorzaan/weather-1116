export const ROADMAP_MAP_TYPE = 'roadmap';
export const SATELLITE_MAP_TYPE = 'satellite';
export const HYBRID_MAP_TYPE = 'hybrid';
export const TERRAIN_MAP_TYPE = 'terrain';

/**
 * Google's built-in type control supports four MapTypeId values:
 * Roadmap, Satellite (aerial), Hybrid (satellite + labels; the light-theme
 * default), and Terrain. Satellite/Hybrid imagery ignores our JSON map
 * styles, so the dark theme defaults to Roadmap ("Map") instead, which is
 * the only type that actually renders themed.
 */
export function defaultMapTypeId(maps, mapTypeId, resolvedTheme) {
  if (mapTypeId) {
    return mapTypeId;
  }
  if (resolvedTheme === 'dark') {
    return maps?.MapTypeId?.ROADMAP ?? ROADMAP_MAP_TYPE;
  }
  return maps?.MapTypeId?.HYBRID ?? HYBRID_MAP_TYPE;
}

export function builtInMapTypeIds(maps) {
  const ids = maps?.MapTypeId;
  return [
    ids?.ROADMAP ?? ROADMAP_MAP_TYPE,
    ids?.SATELLITE ?? SATELLITE_MAP_TYPE,
    ids?.HYBRID ?? HYBRID_MAP_TYPE,
    ids?.TERRAIN ?? TERRAIN_MAP_TYPE,
  ];
}

export function createMapTypeControlOptions(maps) {
  return {
    style: maps?.MapTypeControlStyle?.HORIZONTAL_BAR ?? 'HORIZONTAL_BAR',
    position: maps?.ControlPosition?.TOP_LEFT ?? 'TOP_LEFT',
    mapTypeIds: builtInMapTypeIds(maps),
  };
}
