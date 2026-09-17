# weather-mcp-srv-python

Standalone Python MCP server exposing two public weather tools, backed directly by the
[Open-Meteo](https://open-meteo.com/) API:

- `GetPublicWeatherForecast` — upcoming forecast (`Daily`, `Hourly`, or `FifteenMinutes` resolution)
- `GetPublicWeatherHistory` — recent past weather (`Daily` or `Hourly` resolution)

The `resolution` values are PascalCase (`Daily`/`Hourly`/`FifteenMinutes`) to match what
these tools returned when `GetPublicWeatherForecast`/`GetPublicWeatherHistory` still lived on
`mcp-srv-app-service`, so existing callers/prompts don't need to change.

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
with no children. `isHealthy` is `true` only when `MCP_SRV_PYTHON_KEY` is set and both
`GetPublicWeatherForecast`/`GetPublicWeatherHistory` are registered. `buildNumber`,
`buildStart`, and `buildBranchName` are read from the `BUILD_NUMBER`/`BUILD_START`/
`BUILD_BRANCH_NAME` env vars set by the deploy workflow, same as the other hosts. This is
what api-dotnet/mvc-dotnet's own `/About` fan out to.

## Docker

Built from the repo root so it can reach `mcp-srv-python/`:

```bash
docker build -f mcp-srv-python/mcp/Dockerfile -t weather-mcp-srv-python .
docker run -p 8080:8080 -e MCP_SRV_PYTHON_KEY=... weather-mcp-srv-python
```
