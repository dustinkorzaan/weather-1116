import request from 'supertest';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { buildApp } from '../mcp/src/server.ts';

const MCP_HEADERS = {
  Authorization: 'Bearer test-key',
  'Content-Type': 'application/json',
  Accept: 'application/json, text/event-stream',
};

function buildTestApp(token: string | null = 'test-key') {
  if (token === null) {
    vi.stubEnv('MCP_SRV_NODE_KEY', '');
  } else {
    vi.stubEnv('MCP_SRV_NODE_KEY', token);
  }
  return buildApp();
}

describe('mcp-srv-node', () => {
  beforeEach(() => {
    vi.stubEnv('BUILD_NUMBER', '');
    vi.stubEnv('BUILD_START', '');
    vi.stubEnv('BUILD_BRANCH_NAME', '');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('serves /Wake without auth', async () => {
    const response = await request(buildTestApp()).get('/Wake');
    expect(response.status).toBe(200);
    expect(response.text).toBe('OK');
  });

  it('requires a bearer token on /mcp', async () => {
    const response = await request(buildTestApp()).post('/mcp');
    expect(response.status).toBe(401);
  });

  it('rejects a wrong token on /mcp', async () => {
    const response = await request(buildTestApp()).post('/mcp').set('Authorization', 'Bearer wrong-token');
    expect(response.status).toBe(401);
  });

  it('rejects every /mcp request when the key is unset', async () => {
    const response = await request(buildTestApp(null)).post('/mcp').set('Authorization', 'Bearer ');
    expect(response.status).toBe(401);
  });

  it('accepts a non-localhost Host header (ACA FQDN) after bearer auth', async () => {
    const response = await request(buildTestApp())
      .post('/mcp')
      .set(MCP_HEADERS)
      .set('Host', 'wx1116-prod-mcp-srv-node.example.azurecontainerapps.io')
      .send({ jsonrpc: '2.0', id: 1, method: 'tools/list' });
    expect(response.status).not.toBe(421);
    expect(response.status).not.toBe(401);
  });

  it('lists both weather tools with PascalCase resolutions', async () => {
    const response = await request(buildTestApp())
      .post('/mcp')
      .set(MCP_HEADERS)
      .send({ jsonrpc: '2.0', id: 1, method: 'tools/list' });
    expect(response.status).toBe(200);

    const payload = JSON.parse(response.text.match(/^data: (.*)$/m)?.[1] ?? response.text);
    const tools = payload.result.tools as Array<{ name: string; inputSchema: { properties: Record<string, { enum?: string[] }>; required?: string[] } }>;
    const byName = Object.fromEntries(tools.map((tool) => [tool.name, tool]));

    expect(Object.keys(byName).sort()).toEqual(['GetPublicWeatherForecast', 'GetPublicWeatherHistory']);
    expect(byName.GetPublicWeatherForecast.inputSchema.properties.resolution.enum).toEqual(['Daily', 'Hourly', 'FifteenMinutes']);
    expect(byName.GetPublicWeatherHistory.inputSchema.properties.resolution.enum).toEqual(['Daily', 'Hourly']);
    expect(byName.GetPublicWeatherForecast.inputSchema.required).toEqual(['latitude', 'longitude']);
  });

  it('calls GetPublicWeatherHistory end to end and normalizes the Open-Meteo payload', async () => {
    const fetchMock = vi.fn(async () =>
      Response.json({ latitude: 36.17, daily: { time: ['2026-01-01'], precipitation_sum: [-0.1], weather_code: null } }),
    );
    vi.stubGlobal('fetch', fetchMock);
    try {
      const response = await request(buildTestApp())
        .post('/mcp')
        .set(MCP_HEADERS)
        .send({
          jsonrpc: '2.0',
          id: 1,
          method: 'tools/call',
          params: { name: 'GetPublicWeatherHistory', arguments: { latitude: 36.166, longitude: -86.784 } },
        });
      expect(response.status).toBe(200);

      const payload = JSON.parse(response.text.match(/^data: (.*)$/m)?.[1] ?? response.text);
      expect(payload.result.isError).toBeUndefined();
      const data = JSON.parse(payload.result.content[0].text);
      expect(data.daily.precipitation_sum).toEqual([0]);
      expect(data.daily.weather_code).toEqual([]);
      expect(String(fetchMock.mock.calls[0][0])).toContain('past_days=7');
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it('returns 405 for GET /mcp in stateless mode', async () => {
    const response = await request(buildTestApp()).get('/mcp').set('Authorization', 'Bearer test-key');
    expect(response.status).toBe(405);
  });

  it('serves /About without auth and reports healthy when the key is set', async () => {
    const response = await request(buildTestApp()).get('/About');
    expect(response.status).toBe(200);
    expect(response.body.name).toBe('mcp-srv-node');
    expect(response.body.isHealthy).toBe(true);
    expect(response.body.children).toEqual([]);
  });

  it('reports unhealthy in /About when the key is unset', async () => {
    const response = await request(buildTestApp(null)).get('/About');
    expect(response.status).toBe(200);
    expect(response.body.isHealthy).toBe(false);
  });

  it('reports build metadata in /About', async () => {
    vi.stubEnv('BUILD_NUMBER', '42');
    vi.stubEnv('BUILD_START', '2026-01-02T03:04:05Z');
    vi.stubEnv('BUILD_BRANCH_NAME', 'main');
    const body = (await request(buildTestApp()).get('/About')).body;
    expect(body.buildNumber).toBe(42);
    expect(body.buildStart).toBe('2026-01-02T03:04:05Z');
    expect(body.buildBranchName).toBe('main');
  });

  it('reports null build metadata when unset', async () => {
    const body = (await request(buildTestApp()).get('/About')).body;
    expect(body.buildNumber).toBeNull();
    expect(body.buildStart).toBeNull();
    expect(body.buildBranchName).toBeNull();
  });
});
