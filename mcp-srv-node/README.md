# weather-mcp-srv-node

Standalone Node.js MCP server exposing two public weather tools, backed directly by the
[Open-Meteo](https://open-meteo.com/) API:

- `GetPublicWeatherForecast` — upcoming forecast (`Daily`, `Hourly`, or `FifteenMinutes` resolution)
- `GetPublicWeatherHistory` — recent past weather (`Daily` or `Hourly` resolution)

These two tools used to live on [`mcp-srv-python`](../mcp-srv-python) (which now serves
`GetCities`); this server kept the same tool names, descriptions, PascalCase `resolution` values,
Open-Meteo queries, and response shape, so callers and prompts didn't change when they moved.

This project has no dependency on the rest of this repo (no shared `core` project, no caching
layer) — it's a self-contained MCP server you can build, run, and deploy on its own.

## Stack

- Node 24+ running TypeScript directly via Node's built-in type stripping (no build step;
  `tsc --noEmit` is type-check only)
- [`@modelcontextprotocol/sdk`](https://www.npmjs.com/package/@modelcontextprotocol/sdk)
  streamable-HTTP transport in stateless mode, hosted on Express 5
- `zod` tool input schemas, Vitest + supertest tests

## Running locally

```bash
cd mcp-srv-node
npm ci
cp mcp/.env.example .env  # set MCP_SRV_NODE_KEY
npm start
```

The server listens on `http://0.0.0.0:8150/mcp` by default (override with
`MCP_SRV_NODE_HOST` / `MCP_SRV_NODE_PORT`). `.env` is read from the current directory.

## Tests

```bash
cd mcp-srv-node
npm ci
npm run typecheck
npm test -- --run
```

## Auth

Every request to `/mcp` must carry `Authorization: Bearer <MCP_SRV_NODE_KEY>`. Requests with a
missing, malformed, or incorrect token get a `401`. There is no default token — if
`MCP_SRV_NODE_KEY` isn't set, all `/mcp` requests are rejected.

## Wake

`GET /Wake` is an unauthenticated liveness probe (used to prewarm this container from a
scaled-to-zero state) — it does no tool resolution and doesn't check `MCP_SRV_NODE_KEY`.

## About

`GET /About` is an unauthenticated health probe returning the same `AboutNode` JSON shape
(`Core.About.AboutNode`) as this repo's other backends — a leaf node named `mcp-srv-node` with no
children. `isHealthy` is `true` only when `MCP_SRV_NODE_KEY` is set and every tool in
`EXPECTED_TOOLS` is registered. `buildNumber`, `buildStart`, and `buildBranchName` are read from
the `BUILD_NUMBER`/`BUILD_START`/`BUILD_BRANCH_NAME` env vars set by the deploy workflow, same as
the other hosts. This is what api-dotnet/mvc-dotnet's own `/About` fan out to.

## Observability

When `APPLICATIONINSIGHTS_CONNECTION_STRING` is set (by `infra/modules/container-app.bicep` in
deployed environments), this server exports traces, metrics, and logs to Application Insights via
the [Azure Monitor OpenTelemetry Distro](https://www.npmjs.com/package/@azure/monitor-opentelemetry)
— the same opt-in behavior as the other MCP hosts. It's unset for local dev and test runs, so no
telemetry is sent and no App Insights resource is required.

## Docker

Built from the repo root so it can reach `mcp-srv-node/`:

```bash
docker build -f mcp-srv-node/mcp/Dockerfile -t weather-mcp-srv-node .
docker run -p 8080:8080 -e MCP_SRV_NODE_KEY=... weather-mcp-srv-node
```
