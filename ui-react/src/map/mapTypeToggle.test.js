import { expect, test } from 'vitest';
import {
  HYBRID_MAP_TYPE,
  ROADMAP_MAP_TYPE,
  SATELLITE_MAP_TYPE,
  TERRAIN_MAP_TYPE,
  builtInMapTypeIds,
  createMapTypeControlOptions,
  defaultMapTypeId,
} from './mapTypeToggle';

const maps = {
  MapTypeId: { ROADMAP: 'roadmap', SATELLITE: 'satellite', HYBRID: 'hybrid', TERRAIN: 'terrain' },
  MapTypeControlStyle: { HORIZONTAL_BAR: 'HORIZONTAL_BAR', DROPDOWN_MENU: 'DROPDOWN_MENU' },
  ControlPosition: { TOP_LEFT: 'TOP_LEFT' },
};

const ALL_BUILT_IN_TYPES = [ROADMAP_MAP_TYPE, SATELLITE_MAP_TYPE, HYBRID_MAP_TYPE, TERRAIN_MAP_TYPE];

test('hybrid (satellite with labels) is the default map type', () => {
  expect(defaultMapTypeId(maps)).toBe(HYBRID_MAP_TYPE);
  expect(defaultMapTypeId({})).toBe(HYBRID_MAP_TYPE);
  expect(defaultMapTypeId(maps, SATELLITE_MAP_TYPE)).toBe(SATELLITE_MAP_TYPE);
  expect(defaultMapTypeId(maps, ROADMAP_MAP_TYPE)).toBe(ROADMAP_MAP_TYPE);
});

test('type control offers Google\'s four built-in map types', () => {
  expect(builtInMapTypeIds(maps)).toEqual(ALL_BUILT_IN_TYPES);
  expect(createMapTypeControlOptions(maps)).toEqual({
    style: 'HORIZONTAL_BAR',
    position: 'TOP_LEFT',
    mapTypeIds: ALL_BUILT_IN_TYPES,
  });
  expect(createMapTypeControlOptions({}).mapTypeIds).toEqual(ALL_BUILT_IN_TYPES);
});
