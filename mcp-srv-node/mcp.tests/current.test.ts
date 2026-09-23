import { describe, expect, it } from 'vitest';
import { buildCurrentUrl } from '../mcp/src/tools/current.ts';

describe('buildCurrentUrl', () => {
  it('builds a current-weather URL with metric units and auto timezone', () => {
    const url = buildCurrentUrl(36.1627, -86.7816);
    expect(url.startsWith('https://api.open-meteo.com/v1/forecast?')).toBe(true);
    expect(url).toContain('latitude=36.1627');
    expect(url).toContain('longitude=-86.7816');
    expect(url).toContain('current_weather=true');
    expect(url).toContain('temperature_unit=celsius');
    expect(url).toContain('wind_speed_unit=kmh');
    expect(url).toContain('precipitation_unit=mm');
    expect(url).toContain('timezone=auto');
  });
});
