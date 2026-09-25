"""Python port of FoundryConsoleV4: Responses API with remote MCP servers as tools.

The service calls the four deployed MCP servers itself, so there is no local tool-call loop.
"""

import json
import os
from pathlib import Path
from typing import Any

from dotenv import load_dotenv
from openai import OpenAI

ENDPOINT = "https://wx1116prod2th7yydhws5h6.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1"
DEPLOYMENT_NAME = "gpt-5.4-mini"

# Hardcoded prod ACA FQDNs (wx1116-prod stack). Edit here when reprovisioning
# to a different environment's MCP_SRV_*_HOSTNAME azd outputs.
MCP_SRV_FUNC_APP_URL = "https://wx1116-prod-mcp-srv-func-app.thankfulrock-0d49c0fe.centralus.azurecontainerapps.io/runtime/webhooks/mcp"
MCP_SRV_APP_SERVICE_URL = "https://wx1116-prod-mcp-srv-app-service.thankfulrock-0d49c0fe.centralus.azurecontainerapps.io/mcp"
MCP_SRV_PYTHON_URL = "https://wx1116-prod-mcp-srv-python.thankfulrock-0d49c0fe.centralus.azurecontainerapps.io/mcp"
MCP_SRV_NODE_URL = "https://wx1116-prod-mcp-srv-node.thankfulrock-0d49c0fe.centralus.azurecontainerapps.io/mcp"

COMPASS_POINTS = [
    "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
    "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW",
]

AI_OUTPUT_SCHEMA: dict[str, Any] = {
    "type": "object",
    "properties": {
        "fullSummary": {"type": "string"},
        "temperatureF": {"type": "number"},
        "windSpeedMPH": {"type": "number"},
        "windDirectionSourceDegrees": {"type": "integer"},
        "windDirectionSource": {"type": "string"},
        "conditions": {"type": "string"},
        "latitude": {"type": "number"},
        "longitude": {"type": "number"},
    },
    "required": [
        "fullSummary", "temperatureF", "windSpeedMPH", "windDirectionSourceDegrees",
        "windDirectionSource", "conditions", "latitude", "longitude",
    ],
    "additionalProperties": False,
}


def main() -> None:
    load_dotenv(Path(__file__).with_name(".env"))

    location = "Nashville, TN"

    get_weather_with_mcp_tools(location)


def get_weather_with_mcp_tools(location: str) -> None:
    clear_console()
    print(f"""Example 4
 - Ask AI "What is the current weather in {location}?"
 - Model Direct (using the OpenAI Responses API against unified AI services endpoint)
 - Tools target remote MCP servers instead of in-process tool callbacks
 - The service calls the MCP servers, so there is no local tool-call loop
 - JSON output from AI""")

    # AI prep
    system_prompt = """You are a helpful weather assistant.
Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
You have access to MCP tools for location mapping and real-time public meteorology data.

# Tool Protocol
1. When given a location, immediately call your coordinates resolution tool. It returns ranked matches (rank 1 is best); select the single best-matching place using name, state, and country — normally rank 1, but you may skip rank 1 when a lower rank is clearly correct.
2. Use the latitude and longitude from the best result (normally rank 1) to invoke your weather fetching tool. Fetch weather for that location only — do not query multiple matches.
3. You must query these tools whenever real weather data is required to fulfill the request.

Return valid JSON with these fields:
- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
- temperatureF (number) in Fahrenheit
- windSpeedMPH (number) in MPH
- windDirectionSourceDegrees (integer): Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
- windDirectionSource (string): 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
- conditions (string)
- latitude (number): Decimal degrees from the best geo result (positive north, negative south).
- longitude (number): Decimal degrees from the best geo result (positive east, negative west).

You only return valid JSON."""
    user_prompt = f"What is the current weather today in: {location}?"

    print("\nSystem Prompt:")
    print(system_prompt)
    print("\nUser Prompt:")
    print(user_prompt)
    print("\nAI Output Schema:")
    print(json.dumps(AI_OUTPUT_SCHEMA, indent=2))

    try:
        client = OpenAI(base_url=ENDPOINT, api_key=require_env("AZURE_FOUNDRY_PROD_KEY", "API key not found in environment variables."))
        mcp_tools = build_mcp_tools()

        print("\nMCP Servers:")
        for tool in mcp_tools:
            print(f"{tool['server_label']} {tool['server_url']}")

        response = client.responses.create(
            model=DEPLOYMENT_NAME,
            input=[
                message_item("system", system_prompt),
                message_item("user", user_prompt),
            ],
            tools=mcp_tools,
            text={
                "format": {
                    "type": "json_schema",
                    "name": "ai_weather_response",
                    "schema": AI_OUTPUT_SCHEMA,
                    "strict": True,
                }
            },
        )

        ai_weather = parse_ai_weather(response.output_text)
        if ai_weather is None:
            print("Received empty or invalid JSON response.")
        else:
            print("\nResponse:")
            print(json.dumps(ai_weather, indent=2, ensure_ascii=False))
    except Exception as ex:
        print(f"Request failed: {ex}")

    pause()


def message_item(role: str, text: str) -> dict[str, Any]:
    """An explicit Responses input message. The Foundry endpoint rejects role-only shorthand
    items without "type": "message" (400 invalid_value on input[n].type)."""
    return {"type": "message", "role": role, "content": text}


def build_mcp_tools() -> list[dict[str, Any]]:
    """The four remote MCP servers. The Function App host takes its key in x-functions-key;
    the other three take a Bearer token."""
    func_app_key = require_env("MCP_SRV_FUNC_APP_KEY")
    app_service_key = require_env("MCP_SRV_APP_SERVICE_KEY")
    python_key = require_env("MCP_SRV_PYTHON_KEY")
    node_key = require_env("MCP_SRV_NODE_KEY")

    return [
        mcp_tool("McpSrvFuncApp", MCP_SRV_FUNC_APP_URL, {"x-functions-key": func_app_key}),
        mcp_tool("McpSrvAppService", MCP_SRV_APP_SERVICE_URL, {"Authorization": f"Bearer {app_service_key}"}),
        mcp_tool("McpSrvPython", MCP_SRV_PYTHON_URL, {"Authorization": f"Bearer {python_key}"}),
        mcp_tool("McpSrvNode", MCP_SRV_NODE_URL, {"Authorization": f"Bearer {node_key}"}),
    ]


def mcp_tool(server_label: str, server_url: str, headers: dict[str, str]) -> dict[str, Any]:
    return {
        "type": "mcp",
        "server_label": server_label,
        "server_url": server_url,
        "headers": headers,
        "require_approval": "never",
    }


def require_env(name: str, message: str | None = None) -> str:
    value = os.environ.get(name)
    if not value:
        raise RuntimeError(message or f"{name} not found in environment variables.")
    return value


def parse_ai_weather(content: str | None) -> dict[str, Any] | None:
    """Parse the model's JSON and recompute the compass label from the normalized degrees,
    like the C# console does with WeatherUnitConversion."""
    if not content:
        return None
    ai_weather = json.loads(content)
    if not isinstance(ai_weather, dict):
        return None
    degrees = normalize_source_degrees(int(ai_weather.get("windDirectionSourceDegrees") or 0))
    ai_weather["windDirectionSourceDegrees"] = degrees
    ai_weather["windDirectionSource"] = degrees_to_compass(degrees)
    return ai_weather


def normalize_source_degrees(degrees: int) -> int:
    """Normalizes meteorological source degrees to 0–360."""
    return degrees % 360


def degrees_to_compass(degrees: int) -> str:
    """Converts normalized source degrees (0–360) to a 16-point compass abbreviation.
    round() is banker's rounding, same as C#'s Math.Round."""
    return COMPASS_POINTS[round(degrees / 22.5) % 16]


def clear_console() -> None:
    print("\033[2J\033[H", end="")


def pause() -> None:
    input("\nPress Enter to continue.")


if __name__ == "__main__":
    main()
