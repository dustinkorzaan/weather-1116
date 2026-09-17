"""Standalone MCP server exposing public weather forecast and history tools."""

import os

import uvicorn
from dotenv import load_dotenv, find_dotenv
from mcp.server.mcpserver import MCPServer
from starlette.requests import Request
from starlette.responses import PlainTextResponse

from weather_mcp_srv_python.auth import BearerTokenMiddleware
from weather_mcp_srv_python.tools.forecast import ForecastResolution, get_public_weather_forecast
from weather_mcp_srv_python.tools.history import HistoryResolution, get_public_weather_history

load_dotenv(find_dotenv(usecwd=True))

mcp = MCPServer("WeatherMcpSrvPython")


@mcp.custom_route("/Wake", methods=["GET"])
async def wake(request: Request) -> PlainTextResponse:
    """Anonymous liveness probe for waking this container from zero (no tool resolution, no auth)."""
    return PlainTextResponse("OK")


@mcp.tool(
    name="GetPublicWeatherForecast",
    description=(
        "Get an upcoming public weather forecast for a latitude and longitude. daily is the next 7 "
        "days, hourly is the next 48 hours, and fifteen_minutes is the next 48 hours in 15-minute "
        "steps. Use daily unless the user asks for hourly or 15-minute detail."
    ),
)
async def get_public_weather_forecast_tool(
    latitude: float,
    longitude: float,
    resolution: ForecastResolution = "daily",
) -> dict:
    return await get_public_weather_forecast(latitude, longitude, resolution)


@mcp.tool(
    name="GetPublicWeatherHistory",
    description=(
        "Get recent past public weather for a latitude and longitude. daily is the previous 7 days, "
        "hourly is the previous 48 hours. Use daily unless the user asks for hourly detail."
    ),
)
async def get_public_weather_history_tool(
    latitude: float,
    longitude: float,
    resolution: HistoryResolution = "daily",
) -> dict:
    return await get_public_weather_history(latitude, longitude, resolution)


def build_app():
    """Build the ASGI app: the MCP streamable-HTTP app wrapped with bearer-token auth."""
    token = os.environ.get("MCP_SRV_PYTHON_KEY", "")
    # Stateless mode is enough for simple tool calls (no sampling/elicitation).
    app = mcp.streamable_http_app(stateless_http=True)
    app.add_middleware(BearerTokenMiddleware, token=token, protected_path_prefix="/mcp")
    return app


def main() -> None:
    host = os.environ.get("MCP_SRV_PYTHON_HOST", "0.0.0.0")
    port = int(os.environ.get("MCP_SRV_PYTHON_PORT", "8140"))
    uvicorn.run(build_app(), host=host, port=port)


if __name__ == "__main__":
    main()
