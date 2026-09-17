# weather-mcp-srv-python

Standalone Python MCP server exposing two public weather tools, backed directly by the
[Open-Meteo](https://open-meteo.com/) API:

- `GetPublicWeatherForecast` — upcoming forecast (daily, hourly, or 15-minute resolution)
- `GetPublicWeatherHistory` — recent past weather (daily or hourly resolution)

This project has no dependency on the rest of this repo (no shared `core` project, no
caching layer) — it's a self-contained MCP server you can build, run, and deploy on its
own.

## Running locally

```bash
cd mcp-srv-python
pip install -e .
cp .env.example .env  # set MCP_SRV_PYTHON_KEY
weather-mcp-srv-python
```

The server listens on `http://0.0.0.0:8120/mcp` by default (override with
`MCP_SRV_PYTHON_HOST` / `MCP_SRV_PYTHON_PORT`).

## Auth

Every request to `/mcp` must carry `Authorization: Bearer <MCP_SRV_PYTHON_KEY>`. Requests
with a missing, malformed, or incorrect token get a `401`. There is no default token — if
`MCP_SRV_PYTHON_KEY` isn't set, all `/mcp` requests are rejected.

## Docker

Built from the repo root so it can reach `mcp-srv-python/`:

```bash
docker build -f mcp-srv-python/Dockerfile -t weather-mcp-srv-python .
docker run -p 8080:8080 -e MCP_SRV_PYTHON_KEY=... weather-mcp-srv-python
```
