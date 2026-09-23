import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StreamableHTTPServerTransport } from '@modelcontextprotocol/sdk/server/streamableHttp.js';
import express, { type Express } from 'express';
import { z } from 'zod';
import { bearerTokenAuth } from './auth.ts';
import { FORECAST_RESOLUTIONS, getPublicWeatherForecast } from './tools/forecast.ts';
import { getPublicWeatherHistory, HISTORY_RESOLUTIONS } from './tools/history.ts';

const SERVER_INFO = { name: 'WeatherMcpSrvNode', version: '1.0.0' };

// Every tool this host serves. Comment an entry out (and drop it from EXPECTED_TOOLS) to stop
// serving it here -- two MCP hosts must never register the same tool name.
export const TOOLS = [
  {
    name: 'GetPublicWeatherForecast',
    register: (server: McpServer) =>
      server.registerTool(
        'GetPublicWeatherForecast',
        {
          description:
            'Get an upcoming public weather forecast for a latitude and longitude. Daily is the next 7 ' +
            'days, Hourly is the next 48 hours, and FifteenMinutes is the next 48 hours in 15-minute ' +
            'steps. Use Daily unless the user asks for hourly or 15-minute detail.',
          inputSchema: {
            latitude: z.number(),
            longitude: z.number(),
            resolution: z.enum(FORECAST_RESOLUTIONS).default('Daily'),
          },
        },
        async ({ latitude, longitude, resolution }) =>
          toToolResult(await getPublicWeatherForecast(latitude, longitude, resolution)),
      ),
  },
  {
    name: 'GetPublicWeatherHistory',
    register: (server: McpServer) =>
      server.registerTool(
        'GetPublicWeatherHistory',
        {
          description:
            'Get recent past public weather for a latitude and longitude. Daily is the previous 7 days, ' +
            'Hourly is the previous 48 hours. Use Daily unless the user asks for hourly detail.',
          inputSchema: {
            latitude: z.number(),
            longitude: z.number(),
            resolution: z.enum(HISTORY_RESOLUTIONS).default('Daily'),
          },
        },
        async ({ latitude, longitude, resolution }) =>
          toToolResult(await getPublicWeatherHistory(latitude, longitude, resolution)),
      ),
  },
];

// Tools this host must have registered to report healthy in /About, mirroring
// mcp-srv-python's EXPECTED_TOOLS and mcp-srv-app-service's AboutController.
export const EXPECTED_TOOLS = new Set(['GetPublicWeatherForecast', 'GetPublicWeatherHistory']);

function toToolResult(data: Record<string, unknown>) {
  return {
    content: [{ type: 'text' as const, text: JSON.stringify(data) }],
    structuredContent: data,
  };
}

function createMcpServer(): McpServer {
  const server = new McpServer(SERVER_INFO);
  for (const tool of TOOLS) {
    tool.register(server);
  }
  return server;
}

function parseBuildNumber(value: string | undefined): number | null {
  return value && /^\d+$/.test(value) ? Number(value) : null;
}

/** Build the Express app: stateless MCP streamable-HTTP on /mcp behind bearer auth, plus /Wake and /About. */
export function buildApp(): Express {
  const token = process.env.MCP_SRV_NODE_KEY ?? '';
  const app = express();

  // Anonymous liveness probe for waking this container from zero (no tool resolution, no auth).
  app.get('/Wake', (_req, res) => {
    res.type('text/plain').send('OK');
  });

  // Anonymous About probe -- leaf AboutNode (Core.About.AboutNode shape) named mcp-srv-node,
  // no children. Healthy only when MCP_SRV_NODE_KEY is set and every EXPECTED_TOOLS entry is registered.
  app.get('/About', (_req, res) => {
    const registered = new Set(TOOLS.map((tool) => tool.name));
    const isHealthy = Boolean(process.env.MCP_SRV_NODE_KEY) && [...EXPECTED_TOOLS].every((name) => registered.has(name));

    res.json({
      name: 'mcp-srv-node',
      publicMessage: null,
      isHealthy,
      buildStart: process.env.BUILD_START || null,
      buildNumber: parseBuildNumber(process.env.BUILD_NUMBER),
      buildBranchName: process.env.BUILD_BRANCH_NAME || null,
      children: [],
    });
  });

  app.use(bearerTokenAuth(token, '/mcp'));

  // Stateless mode is enough for simple tool calls (no sampling/elicitation): a fresh server and
  // transport per request, no session IDs. No Host-header allow-list is configured, so ACA FQDNs
  // are accepted; ingress handles edge Host checks and /mcp is already behind bearer auth.
  app.post('/mcp', express.json(), async (req, res) => {
    const server = createMcpServer();
    const transport = new StreamableHTTPServerTransport({ sessionIdGenerator: undefined });
    res.on('close', () => {
      void transport.close();
      void server.close();
    });
    await server.connect(transport);
    await transport.handleRequest(req, res, req.body);
  });

  app.all('/mcp', (_req, res) => {
    res.status(405).set('Allow', 'POST').type('text/plain').send('Method Not Allowed');
  });

  return app;
}

export function main(): void {
  const host = process.env.MCP_SRV_NODE_HOST || '0.0.0.0';
  const port = Number(process.env.MCP_SRV_NODE_PORT || '8150');
  buildApp().listen(port, host, () => {
    console.log(`weather-mcp-srv-node listening on http://${host}:${port}/mcp`);
  });
}
