import { describe, expect, it } from 'vitest';
import { buildForecastUrl } from '../mcp/src/tools/forecast.ts';
import { normalizeSeriesBlock, SUB_HOURLY_KEYS } from '../mcp/src/tools/openMeteo.ts';

describe('buildForecastUrl', () => {
  it('builds a daily URL with metric units and auto timezone', () => {
    const url = buildForecastUrl(47.6062, -122.3321, 'Daily');
    expect(url.startsWith('https://api.open-meteo.com/v1/forecast?')).toBe(true);
    expect(url).toContain('latitude=47.6062');
    expect(url).toContain('longitude=-122.3321');
    expect(url).toContain('forecast_days=7');
    expect(url).toContain('temperature_2m_max');
    expect(url).toContain('temperature_unit=celsius');
    expect(url).toContain('wind_speed_unit=kmh');
    expect(url).toContain('precipitation_unit=mm');
    expect(url).toContain('timezone=auto');
  });

  it('builds an hourly URL', () => {
    const url = buildForecastUrl(0, 0, 'Hourly');
    expect(url).toContain('forecast_hours=48');
    expect(url).toContain('hourly=');
  });

  it('builds a fifteen-minute URL', () => {
    const url = buildForecastUrl(0, 0, 'FifteenMinutes');
    expect(url).toContain('forecast_minutely_15=192');
    expect(url).toContain('minutely_15=');
  });

  it('rejects an unknown resolution', () => {
    expect(() => buildForecastUrl(0, 0, 'bogus')).toThrow();
  });
});

describe('normalizeSeriesBlock', () => {
  it('fills nulls and clamps precipitation', () => {
    const block: Record<string, unknown> = {
      time: null,
      temperature_2m: [1.0],
      precipitation: [-2.0, 3.0],
      weather_code: null,
      wind_speed_10m: null,
      wind_direction_10m: null,
    };
    normalizeSeriesBlock(block, SUB_HOURLY_KEYS);
    expect(block.time).toEqual([]);
    expect(block.weather_code).toEqual([]);
    expect(block.precipitation).toEqual([0, 3.0]);
  });

  it('handles a missing block', () => {
    expect(() => normalizeSeriesBlock(undefined, SUB_HOURLY_KEYS)).not.toThrow();
  });
});
