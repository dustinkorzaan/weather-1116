# weather-mcp-srv-python

Standalone Python MCP server exposing one geo tool, backed directly by the free (no-key)
[GeoDB Cities](https://wirefreethought.github.io/geodb-rest-api-docs/) service:

- `GetCities(latitude, longitude, distanceKM=161, size=25)` — the largest cities (by population)
  within `distanceKM` of a coordinate, largest first. Each city has `name`, `region`, `country`,
  `latitude`, `longitude`, `distanceKm`, and `population`; the result also carries the effective
  `distanceKm`/`size`, `returned`, and `totalAvailable`.

`distanceKM` is reset into 1–1000 km and `size` into 0–100 on every call — out-of-range values
are adjusted, never rejected. `size` 0 returns an empty list without calling GeoDB. GeoDB's free
tier returns at most 10 cities per request at about 1 request per second, so larger sizes are
fetched page by page (25 cities ≈ 3 requests). The same tool is implemented in-process in Core
(`GetCitiesEvent`/`GetCitiesHandler`) for the local-loop chat paths.

`GetPublicWeatherForecast`/`GetPublicWeatherHistory` used to live here; they are served by
[`mcp-srv-node`](../mcp-srv-node).

This project has no dependency on the rest of this repo (no shared `core` project, no
caching layer) — it's a self-contained MCP server you can build, run, and deploy on its
own.

## Running locally

```bash
cd mcp-srv-python/mcp
pip install -e .
cp .env.example .env  # set MCP_SRV_PYTHON_KEY
weather-mcp-srv-python
```

The server listens on `http://0.0.0.0:8140/mcp` by default (override with
`MCP_SRV_PYTHON_HOST` / `MCP_SRV_PYTHON_PORT`).

## Auth

Every request to `/mcp` must carry `Authorization: Bearer <MCP_SRV_PYTHON_KEY>`. Requests
with a missing, malformed, or incorrect token get a `401`. There is no default token — if
`MCP_SRV_PYTHON_KEY` isn't set, all `/mcp` requests are rejected.

## Wake

`GET /Wake` is an unauthenticated liveness probe (used to prewarm this container from a
scaled-to-zero state) — it does no tool resolution and doesn't check `MCP_SRV_PYTHON_KEY`.

## About

`GET /About` is an unauthenticated health probe returning the same `AboutNode` JSON shape
(`Core.About.AboutNode`) as this repo's other backends — a leaf node named `mcp-srv-python`
with no children. `isHealthy` is `true` only when `MCP_SRV_PYTHON_KEY` is set and every tool
in `EXPECTED_TOOLS` (`GetCities`) is registered. `buildNumber`,
`buildStart`, and `buildBranchName` are read from the `BUILD_NUMBER`/`BUILD_START`/
`BUILD_BRANCH_NAME` env vars set by the deploy workflow, same as the other hosts. This is
what api-dotnet/mvc-dotnet's own `/About` fan out to.

## Observability

When `APPLICATIONINSIGHTS_CONNECTION_STRING` is set (by `infra/modules/container-app.bicep` in
deployed environments), this server exports traces, metrics, and logs to Application Insights via
the [Azure Monitor OpenTelemetry Distro](https://pypi.org/project/azure-monitor-opentelemetry/),
instrumenting incoming Starlette requests and outgoing `httpx` calls to GeoDB — the same
opt-in behavior as `mcp-srv-app-service` and `mcp-srv-func-app`. It's unset for local dev and
`pytest` runs, so no telemetry is sent and no App Insights resource is required.

## Docker

Built from the repo root so it can reach `mcp-srv-python/`:

```bash
docker build -f mcp-srv-python/mcp/Dockerfile -t weather-mcp-srv-python .
docker run -p 8080:8080 -e MCP_SRV_PYTHON_KEY=... weather-mcp-srv-python
```
