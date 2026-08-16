export const ROADMAP_MAP_TYPE = 'roadmap';
export const SATELLITE_MAP_TYPE = 'satellite';

/**
 * Google's type control labels aerial photography "Satellite".
 * Roadmap is the default canvas.
 */
export function defaultMapTypeId(maps, mapTypeId) {
  if (mapTypeId) {
    return mapTypeId;
  }
  return maps?.MapTypeId?.ROADMAP ?? ROADMAP_MAP_TYPE;
}

export function createMapTypeControlOptions(maps) {
  const ids = maps?.MapTypeId;
  return {
    style: maps?.MapTypeControlStyle?.HORIZONTAL_BAR ?? 'HORIZONTAL_BAR',
    position: maps?.ControlPosition?.TOP_LEFT ?? 'TOP_LEFT',
    mapTypeIds: [ids?.ROADMAP ?? ROADMAP_MAP_TYPE, ids?.SATELLITE ?? SATELLITE_MAP_TYPE],
  };
}
