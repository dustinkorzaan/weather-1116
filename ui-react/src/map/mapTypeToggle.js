export const ROADMAP_MAP_TYPE = 'roadmap';
export const SATELLITE_MAP_TYPE = 'satellite';
export const HYBRID_MAP_TYPE = 'hybrid';
export const TERRAIN_MAP_TYPE = 'terrain';

/**
 * Google's built-in type control supports four MapTypeId values:
 * Roadmap (default), Satellite (aerial), Hybrid (satellite + labels),
 * and Terrain.
 */
export function defaultMapTypeId(maps, mapTypeId) {
  if (mapTypeId) {
    return mapTypeId;
  }
  return maps?.MapTypeId?.ROADMAP ?? ROADMAP_MAP_TYPE;
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
