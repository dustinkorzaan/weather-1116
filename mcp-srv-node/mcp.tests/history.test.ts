import { describe, expect, it } from 'vitest';
import { buildHistoryUrl } from '../mcp/src/tools/history.ts';
import { DAILY_KEYS, normalizeSeriesBlock } from '../mcp/src/tools/openMeteo.ts';

describe('buildHistoryUrl', () => {
  it('builds a daily URL for the previous 7 days', () => {
    const url = buildHistoryUrl(47.6062, -122.3321, 'Daily');
    expect(url.startsWith('https://api.open-meteo.com/v1/forecast?')).toBe(true);
    expect(url).toContain('past_days=7');
    expect(url).toContain('forecast_days=0');
  });

  it('builds an hourly URL for the previous 48 hours', () => {
    const url = buildHistoryUrl(0, 0, 'Hourly');
    expect(url).toContain('past_hours=48');
    expect(url).toContain('forecast_hours=0');
  });

  it('rejects an unknown resolution', () => {
    expect(() => buildHistoryUrl(0, 0, 'bogus')).toThrow();
  });
});

describe('normalizeSeriesBlock (daily)', () => {
  it('clamps daily precipitation_sum and fills nulls', () => {
    const block: Record<string, unknown> = {
      time: ['2024-01-01'],
      weather_code: [1],
      temperature_2m_max: [10.0],
      temperature_2m_min: [5.0],
      precipitation_sum: [-1.0, 2.0],
      wind_speed_10m_max: null,
      wind_direction_10m_dominant: null,
    };
    normalizeSeriesBlock(block, DAILY_KEYS);
    expect(block.precipitation_sum).toEqual([0, 2.0]);
    expect(block.wind_speed_10m_max).toEqual([]);
    expect(block.wind_direction_10m_dominant).toEqual([]);
  });
});
