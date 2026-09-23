import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import request from 'supertest';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { buildApp, TOOLS } from '../mcp/src/server.ts';

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

  it('calls GetPublicWeatherForecast end to end with the requested resolution', async () => {
    const fetchMock = vi.fn(async () =>
      Response.json({ latitude: 36.17, hourly: { time: ['2026-01-01T00:00'], precipitation: [-0.2], wind_speed_10m: null } }),
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
          params: { name: 'GetPublicWeatherForecast', arguments: { latitude: 36.166, longitude: -86.784, resolution: 'Hourly' } },
        });
      expect(response.status).toBe(200);

      const payload = JSON.parse(response.text.match(/^data: (.*)$/m)?.[1] ?? response.text);
      expect(payload.result.isError).toBeUndefined();
      const data = JSON.parse(payload.result.content[0].text);
      expect(data.hourly.precipitation).toEqual([0]);
      expect(data.hourly.wind_speed_10m).toEqual([]);
      expect(String(fetchMock.mock.calls[0][0])).toContain('forecast_hours=48');
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it('reports unhealthy in /About when an expected tool is not actually registered', async () => {
    const removed = TOOLS.splice(0, 1);
    try {
      const response = await request(buildTestApp()).get('/About');
      expect(response.status).toBe(200);
      expect(response.body.isHealthy).toBe(false);
    } finally {
      TOOLS.unshift(...removed);
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

describe('mcp-srv-node entry point', () => {
  // Regression: enabling Application Insights must not break startup or routing (mirrors
  // mcp-srv-python's test_build_app_instruments_when_app_insights_connection_string_set).
  it('starts and serves /About with an App Insights connection string set', async () => {
    const port = 18000 + Math.floor(Math.random() * 1000);
    const child = spawn(process.execPath, [fileURLToPath(new URL('../mcp/src/index.ts', import.meta.url))], {
      env: {
        ...process.env,
        MCP_SRV_NODE_KEY: 'test-key',
        MCP_SRV_NODE_HOST: '127.0.0.1',
        MCP_SRV_NODE_PORT: String(port),
        APPLICATIONINSIGHTS_CONNECTION_STRING:
          'InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://fake.example.com/',
      },
      stdio: 'ignore',
    });
    try {
      let body: { name?: string; isHealthy?: boolean } | undefined;
      for (let attempt = 0; attempt < 50 && !body; attempt++) {
        try {
          const response = await fetch(`http://127.0.0.1:${port}/About`);
          body = await response.json();
        } catch {
          await new Promise((resolve) => setTimeout(resolve, 100));
        }
      }
      expect(body?.name).toBe('mcp-srv-node');
      expect(body?.isHealthy).toBe(true);
    } finally {
      child.kill();
    }
  }, 15_000);
});
