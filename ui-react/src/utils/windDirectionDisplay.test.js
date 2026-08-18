import { expect, test } from 'vitest';
import { WIND_DIRECTION_ARROW } from './windDirectionDisplay';

test('uses the down-pointing wind direction arrow glyph', () => {
  expect(WIND_DIRECTION_ARROW).toBe('\u2B9B');
});
