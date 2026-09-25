"""Python port of FoundryConsoleV3: Responses API with in-process tool callbacks.

The model calls two local Python functions (GetLatLong, GetPublicWeatherCurrent) through a
local tool-call loop, then returns strict JSON.

Unlike the C# console, only those two tools are ported: no GetLocation, forecast, history,
or database-backed tools, so this console needs no DB_CONNECTION_STRING.
"""

import json
import os
from pathlib import Path
from typing import Any, Callable
from urllib.parse import urlencode

import httpx
from dotenv import load_dotenv
from openai import OpenAI

ENDPOINT = "https://wx1116prod2th7yydhws5h6.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1"
DEPLOYMENT_NAME = "gpt-5.4-mini"

# LM Studio Bionic Demo (3–5 minutes of startup, followed by 3–5 minutes of interactive use over 3–5 loops
# ENDPOINT = "http://localhost:1234/v1"
# DEPLOYMENT_NAME = "qwen/qwen3.5-9b"

OPEN_METEO_GEOCODING_URL = "https://geocoding-api.open-meteo.com/v1/search"
OPEN_METEO_FORECAST_URL = "https://api.open-meteo.com/v1/forecast"

# Identifying User-Agent; same value as Core's HTTP handlers.
USER_AGENT = "Weather-1116/1.0 (https://github.com/dustinkorzaan/weather-1116)"

GEO_MATCH_COUNT = 5

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

_LAT_LONG_PROPERTIES: dict[str, Any] = {
    "latitude": {"type": "number", "description": "Latitude in decimal degrees"},
    "longitude": {"type": "number", "description": "Longitude in decimal degrees"},
}


def _function_tool(name: str, description: str, properties: dict[str, Any]) -> dict[str, Any]:
    return {
        "type": "function",
        "name": name,
        "description": description,
        "parameters": {
            "type": "object",
            "properties": properties,
            "required": list(properties),
            "additionalProperties": False,
        },
        "strict": True,
    }


TOOLS: list[dict[str, Any]] = [
    _function_tool(
        "GetLatLong",
        "Resolve a location name to ranked latitude/longitude matches using public geocoding data. Returns up to 5 results (rank 1 is the best match). Use state and country to pick the right place if rank 1 is wrong.",
        {"location": {"type": "string", "description": "City and optional region/country, e.g. Nashville, TN"}},
    ),
    _function_tool(
        "GetPublicWeatherCurrent",
        "Get current public weather conditions for a latitude and longitude.",
        _LAT_LONG_PROPERTIES,
    ),
]


def main() -> None:
    load_dotenv(Path(__file__).with_name(".env"))

    location = "Nashville, TN"

    get_weather_json_in_json_out(location)


def get_weather_json_in_json_out(location: str) -> None:
    clear_console()
    print(f"""Example 4
 - Ask AI "What is the current weather in {location}?"
 - Responses API with in-process tool callbacks (GetLatLong, GetPublicWeatherCurrent)
 - Model can call tools to derive lat/long and fetch current public weather
 - JSON output from AI""")

    # AI prep
    system_prompt = """You are a helpful weather assistant.
Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
You can call the GetLatLong tool to resolve a place name to ranked latitude/longitude
matches (up to 5; rank 1 is the best match). Call GetPublicWeatherCurrent for conditions now.

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
        client = OpenAI(base_url=ENDPOINT, api_key=get_api_key())
        final_content = run_tool_loop(client, system_prompt, user_prompt)
        ai_weather = parse_ai_weather(final_content)
        if ai_weather is None:
            print("Received empty or invalid JSON response.")
        else:
            print("\nResponse:")
            print(json.dumps(ai_weather, indent=2, ensure_ascii=False))
    except Exception as ex:
        print(f"Request failed: {ex}")

    pause()


# ---------------------------------------------------------------------------
# Tool loop
# ---------------------------------------------------------------------------


def run_tool_loop(client: Any, system_prompt: str, user_prompt: str) -> str:
    """Call the model, run any function calls it asks for locally, feed the outputs back,
    and repeat until it answers without calling a tool."""
    input_items: list[Any] = [message_item("user", user_prompt)]

    while True:
        print("\nCreating response with options...")
        response = client.responses.create(
            model=DEPLOYMENT_NAME,
            instructions=system_prompt,
            input=input_items,
            tools=TOOLS,
            text={
                "format": {
                    "type": "json_schema",
                    "name": "ai_weather_response",
                    "schema": AI_OUTPUT_SCHEMA,
                    "strict": True,
                }
            },
        )

        print("Adding response output items to input items...")
        input_items.extend(response.output)

        function_calls = [item for item in response.output if getattr(item, "type", None) == "function_call"]
        if not function_calls:
            if not response.output_text:
                raise RuntimeError("Model finished without producing content.")
            return response.output_text

        for function_call in function_calls:
            function_output = call_tool(function_call.name, function_call.arguments)
            input_items.append(
                {"type": "function_call_output", "call_id": function_call.call_id, "output": function_output}
            )


def message_item(role: str, text: str) -> dict[str, Any]:
    """An explicit Responses input message. The Foundry endpoint rejects role-only shorthand
    items without "type": "message" (400 invalid_value on input[n].type)."""
    return {"type": "message", "role": role, "content": text}


def call_tool(name: str, arguments_json: str) -> str:
    """Dispatch one function call to its local implementation and return the JSON output."""
    handler = TOOL_HANDLERS.get(name)
    if handler is None:
        raise NotImplementedError(f"Unexpected tool call: {name}")

    arguments = json.loads(arguments_json)
    print(f"\nTool call: {name}({', '.join(str(value) for value in arguments.values())})")
    function_output = json.dumps(handler(arguments), indent=2)
    print(f"Tool output: {function_output}")
    return function_output


TOOL_HANDLERS: dict[str, Callable[[dict[str, Any]], Any]] = {
    "GetLatLong": lambda args: get_lat_long(args["location"]),
    "GetPublicWeatherCurrent": lambda args: get_public_weather_current(args["latitude"], args["longitude"]),
}


# ---------------------------------------------------------------------------
# AI helpers
# ---------------------------------------------------------------------------


def get_api_key() -> str:
    api_key = os.environ.get("AZURE_FOUNDRY_PROD_KEY")
    if not api_key:
        raise RuntimeError("API key not found in environment variables.")
    return api_key


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


# ---------------------------------------------------------------------------
# GetLatLong (stand-in for Core's GetLatLongHandler)
# ---------------------------------------------------------------------------


def _get_json(url: str) -> Any:
    with httpx.Client(
        timeout=30.0,
        headers={"Accept": "application/json", "User-Agent": USER_AGENT},
        follow_redirects=True,
    ) as client:
        response = client.get(url)
        response.raise_for_status()
        return response.json()


def location_queries(location: str) -> list[str]:
    """The location as given, then (for "City, ST") just the part before the first comma."""
    queries = [location]
    if "," in location:
        city = location.split(",")[0].strip()
        if city.casefold() != location.casefold():
            queries.append(city)
    return queries


def get_lat_long(location: str) -> dict[str, Any]:
    """Ranked Open-Meteo geocoding matches for a location name (rank 1 is the best match)."""
    for query in location_queries(location):
        params = {"name": query, "count": GEO_MATCH_COUNT, "language": "en", "format": "json"}
        matches = (_get_json(f"{OPEN_METEO_GEOCODING_URL}?{urlencode(params)}") or {}).get("results") or []
        if matches:
            return {
                "results": [
                    {
                        "rank": rank,
                        "name": match.get("name") or "",
                        "state": match.get("admin1") or "",
                        "country": match.get("country") or "",
                        "latitude": match.get("latitude") or 0.0,
                        "longitude": match.get("longitude") or 0.0,
                    }
                    for rank, match in enumerate(matches[:GEO_MATCH_COUNT], start=1)
                ]
            }
    raise ValueError(f"Non-AI: No results found for '{location}'.")


# ---------------------------------------------------------------------------
# GetPublicWeatherCurrent (stand-in for Core's GetPublicWeatherCurrentHandler)
# ---------------------------------------------------------------------------


def build_current_weather_url(latitude: float, longitude: float) -> str:
    # Metric units, GMT; the model converts to U.S. customary units. Matches Core's current-weather URL.
    params = {
        "latitude": latitude,
        "longitude": longitude,
        "current_weather": "true",
        "temperature_unit": "celsius",
        "wind_speed_unit": "kmh",
    }
    return f"{OPEN_METEO_FORECAST_URL}?{urlencode(params)}"


def get_public_weather_current(latitude: float, longitude: float) -> dict[str, Any]:
    return _get_json(build_current_weather_url(latitude, longitude))


# ---------------------------------------------------------------------------
# Console helpers
# ---------------------------------------------------------------------------


def clear_console() -> None:
    print("\033[2J\033[H", end="")


def pause() -> None:
    input("\nPress Enter to continue.")


if __name__ == "__main__":
    main()
