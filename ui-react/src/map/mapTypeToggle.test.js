import { expect, test } from 'vitest';
import {
  ROADMAP_MAP_TYPE,
  SATELLITE_MAP_TYPE,
  createMapTypeControlOptions,
  defaultMapTypeId,
} from './mapTypeToggle';

const maps = {
  MapTypeId: { ROADMAP: 'roadmap', SATELLITE: 'satellite', HYBRID: 'hybrid', TERRAIN: 'terrain' },
  MapTypeControlStyle: { HORIZONTAL_BAR: 'HORIZONTAL_BAR', DROPDOWN_MENU: 'DROPDOWN_MENU' },
  ControlPosition: { TOP_LEFT: 'TOP_LEFT' },
};

test('roadmap is the default map type', () => {
  expect(defaultMapTypeId(maps)).toBe(ROADMAP_MAP_TYPE);
  expect(defaultMapTypeId({})).toBe(ROADMAP_MAP_TYPE);
  expect(defaultMapTypeId(maps, SATELLITE_MAP_TYPE)).toBe(SATELLITE_MAP_TYPE);
});

test('type control offers roadmap and satellite (aerial)', () => {
  expect(createMapTypeControlOptions(maps)).toEqual({
    style: 'HORIZONTAL_BAR',
    position: 'TOP_LEFT',
    mapTypeIds: [ROADMAP_MAP_TYPE, SATELLITE_MAP_TYPE],
  });
  expect(createMapTypeControlOptions({}).mapTypeIds).toEqual([ROADMAP_MAP_TYPE, SATELLITE_MAP_TYPE]);
});
