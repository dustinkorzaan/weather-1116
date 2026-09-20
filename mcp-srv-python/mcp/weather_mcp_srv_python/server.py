"""Standalone MCP server exposing public weather forecast and history tools."""

import os

import uvicorn
from dotenv import load_dotenv, find_dotenv
from mcp.server.mcpserver import MCPServer
from mcp.server.transport_security import TransportSecuritySettings
from starlette.requests import Request
from starlette.responses import JSONResponse, PlainTextResponse

from weather_mcp_srv_python.auth import BearerTokenMiddleware
from weather_mcp_srv_python.tools.forecast import ForecastResolution, get_public_weather_forecast
from weather_mcp_srv_python.tools.history import HistoryResolution, get_public_weather_history

load_dotenv(find_dotenv(usecwd=True))


def _app_insights_enabled() -> bool:
    """Mirrors .NET's IsNullOrWhiteSpace guard: a whitespace-only connection string
    is treated as unset rather than passed to configure_azure_monitor(), which
    would otherwise raise."""
    return bool(os.environ.get("APPLICATIONINSIGHTS_CONNECTION_STRING", "").strip())


# Exports traces/metrics/logs to Application Insights via APPLICATIONINSIGHTS_CONNECTION_STRING
# (set by infra/modules/container-app.bicep), mirroring mcp-srv-app-service and mcp-srv-func-app.
# configure_azure_monitor() raises ValueError when the connection string is missing, so it's
# opt-in -- local dev and pytest runs have no App Insights resource at all.
if _app_insights_enabled():
    from azure.monitor.opentelemetry import configure_azure_monitor
    from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor

    configure_azure_monitor()
    HTTPXClientInstrumentor().instrument()

mcp = MCPServer("WeatherMcpSrvPython")

# Tools this host must have registered to report healthy in /About, mirroring
# mcp-srv-app-service's AboutController and mcp-srv-func-app's AboutFunction.
EXPECTED_TOOLS = {"GetPublicWeatherForecast", "GetPublicWeatherHistory"}


@mcp.custom_route("/Wake", methods=["GET"])
async def wake(request: Request) -> PlainTextResponse:
    """Anonymous liveness probe for waking this container from zero (no tool resolution, no auth)."""
    return PlainTextResponse("OK")


@mcp.custom_route("/About", methods=["GET"])
async def about(request: Request) -> JSONResponse:
    """Anonymous About probe -- leaf AboutNode (Core.About.AboutNode shape) named
    mcp-srv-python, no children. Healthy only when MCP_SRV_PYTHON_KEY is set and both
    weather tools are registered."""
    key = os.environ.get("MCP_SRV_PYTHON_KEY", "")
    tools = await mcp.list_tools()
    tool_names = {tool.name for tool in tools}
    is_healthy = bool(key) and EXPECTED_TOOLS.issubset(tool_names)

    build_number = os.environ.get("BUILD_NUMBER", "")

    return JSONResponse(
        {
            "name": "mcp-srv-python",
            "publicMessage": None,
            "isHealthy": is_healthy,
            "buildStart": os.environ.get("BUILD_START") or None,
            "buildNumber": int(build_number) if build_number.isdigit() else None,
            "buildBranchName": os.environ.get("BUILD_BRANCH_NAME") or None,
            "children": [],
        }
    )


@mcp.tool(
    name="GetPublicWeatherForecast",
    description=(
        "Get an upcoming public weather forecast for a latitude and longitude. Daily is the next 7 "
        "days, Hourly is the next 48 hours, and FifteenMinutes is the next 48 hours in 15-minute "
        "steps. Use Daily unless the user asks for hourly or 15-minute detail."
    ),
)
async def get_public_weather_forecast_tool(
    latitude: float,
    longitude: float,
    resolution: ForecastResolution = "Daily",
) -> dict:
    return await get_public_weather_forecast(latitude, longitude, resolution)


@mcp.tool(
    name="GetPublicWeatherHistory",
    description=(
        "Get recent past public weather for a latitude and longitude. Daily is the previous 7 days, "
        "Hourly is the previous 48 hours. Use Daily unless the user asks for hourly detail."
    ),
)
async def get_public_weather_history_tool(
    latitude: float,
    longitude: float,
    resolution: HistoryResolution = "Daily",
) -> dict:
    return await get_public_weather_history(latitude, longitude, resolution)


def build_app():
    """Build the ASGI app: the MCP streamable-HTTP app wrapped with bearer-token auth."""
    token = os.environ.get("MCP_SRV_PYTHON_KEY", "")
    # Stateless mode is enough for simple tool calls (no sampling/elicitation).
    # The MCP SDK defaults to localhost-only Host validation (421 on ACA FQDNs).
    # Ingress handles edge Host checks; /mcp is already protected by BearerTokenMiddleware.
    app = mcp.streamable_http_app(
        stateless_http=True,
        transport_security=TransportSecuritySettings(enable_dns_rebinding_protection=False),
    )
    app.add_middleware(BearerTokenMiddleware, token=token, protected_path_prefix="/mcp")

    if _app_insights_enabled():
        from opentelemetry.instrumentation.starlette import StarletteInstrumentor

        StarletteInstrumentor.instrument_app(app)

    return app


def main() -> None:
    host = os.environ.get("MCP_SRV_PYTHON_HOST", "0.0.0.0")
    port = int(os.environ.get("MCP_SRV_PYTHON_PORT", "8140"))
    uvicorn.run(build_app(), host=host, port=port)


if __name__ == "__main__":
    main()
