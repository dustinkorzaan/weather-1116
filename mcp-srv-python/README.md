# weather-mcp-srv-python

Standalone Python MCP server exposing two geo tools, backed directly by free (no-key) public
services:

- `GetLatLong(location)` — resolve a place name (e.g. `Nashville, TN`) to up to 5 ranked
  latitude/longitude matches via [Open-Meteo geocoding](https://open-meteo.com/en/docs/geocoding-api).
  Each match has `rank` (1 is best), `name`, `state`, `country`, `latitude`, and `longitude`. For
  inputs like `City, ST` that return nothing, it retries with just the part before the comma; when
  nothing matches at all the tool call fails with `No results found`.
- `GetLocation(latitude, longitude)` — reverse-geocode a coordinate to a simple `location` label via
  [Nominatim](https://nominatim.org/release-docs/latest/api/Reverse/): `City, State` in the US
  (`City, State, Country` elsewhere), then Nominatim's feature name, then a formatted coordinate such
  as `35.51° N, 86.58° W`.

Both are Python ports of Core's `GetLatLongHandler`/`GetLocationHandler` (same tool names,
descriptions, and response shapes), which still run in-process for the local-loop chat paths.
Throttling (429), server errors (5xx), and transport errors are retried a few times; other 4xx fail
immediately. A `GetLatLong` query variant that still fails does not stop the `City` fallback from
being tried. Successful results are cached in-process for 60 minutes (like Core's handlers), as
Nominatim's usage policy requires.

`GetCities` used to live here; it is served by [`mcp-srv-func-app`](../mcp-srv-func-app) through
Core's `GetCitiesHandler`. `GetPublicWeatherForecast`/`GetPublicWeatherHistory` also used to live
here; they are served by [`mcp-srv-node`](../mcp-srv-node).

This project has no dependency on the rest of this repo (no shared `core` project; its small
in-process cache lives in `tools/geo.py`) — it's a self-contained MCP server you can build, run,
and deploy on its own.

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
in `EXPECTED_TOOLS` (`GetLatLong`, `GetLocation`) is registered. `buildNumber`,
`buildStart`, and `buildBranchName` are read from the `BUILD_NUMBER`/`BUILD_START`/
`BUILD_BRANCH_NAME` env vars set by the deploy workflow, same as the other hosts. This is
what api-dotnet/mvc-dotnet's own `/About` fan out to.

## Observability

When `APPLICATIONINSIGHTS_CONNECTION_STRING` is set (by `infra/modules/container-app.bicep` in
deployed environments), this server exports traces, metrics, and logs to Application Insights via
the [Azure Monitor OpenTelemetry Distro](https://pypi.org/project/azure-monitor-opentelemetry/),
instrumenting incoming Starlette requests and outgoing `httpx` calls to Open-Meteo and Nominatim — the same
opt-in behavior as `mcp-srv-app-service` and `mcp-srv-func-app`. It's unset for local dev and
`pytest` runs, so no telemetry is sent and no App Insights resource is required.

## Docker

Built from the repo root so it can reach `mcp-srv-python/`:

```bash
docker build -f mcp-srv-python/mcp/Dockerfile -t weather-mcp-srv-python .
docker run -p 8080:8080 -e MCP_SRV_PYTHON_KEY=... weather-mcp-srv-python
```
