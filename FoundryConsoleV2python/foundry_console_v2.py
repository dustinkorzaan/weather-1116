"""Python port of FoundryConsoleV2: model direct against the unified AI services endpoint.

Four examples, same prompts as the C# console:
  1. Ask for the weather with no data (expected to fail or refuse).
  2. Ask the model to make something up.
  3. Provide raw Open-Meteo JSON, get a string back.
  4. Provide raw Open-Meteo JSON, get strict JSON back.
"""

import json
import os
import time
from pathlib import Path
from typing import Any
from urllib.parse import urlencode

import httpx
from dotenv import load_dotenv
from openai import OpenAI

ENDPOINT = "https://wx1116prod2th7yydhws5h6.services.ai.azure.com/api/projects/wx1116-prod-proj/openai/v1"
DEPLOYMENT_NAME = "gpt-5.4-mini"

OPEN_METEO_GEOCODING_URL = "https://geocoding-api.open-meteo.com/v1/search"
OPEN_METEO_FORECAST_URL = "https://api.open-meteo.com/v1/forecast"

# Identifying User-Agent; same value as Core's HTTP handlers.
USER_AGENT = "Weather-1116/1.0 (https://github.com/dustinkorzaan/weather-1116)"

ATTEMPTS = 3
RETRY_DELAY_SECONDS = 0.5

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

    get_weather_will_fail(location)
    get_weather_make_up_something(location)

    get_weather_json_in_string_out(location)

    get_weather_json_in_json_out(location)


# ---------------------------------------------------------------------------
# Examples
# ---------------------------------------------------------------------------


def get_weather_will_fail(location: str) -> None:
    clear_console()
    print(f"""Example 1
 - Ask AI "What is the current weather in {location}?"
 - Model Direct (using the OpenAI Responses API against unified AI services endpoint)
 - This is expected to fail because it doesn't have supporting data.""")

    # AI prep
    system_prompt = "You are a helpful weather assistant."
    user_prompt = f"What is the current weather today for {location}?"

    print_prompts(system_prompt, user_prompt)

    try:
        response = create_client().responses.create(
            model=DEPLOYMENT_NAME,
            instructions=system_prompt,
            input=[message_item("user", user_prompt)],
        )
        print("\nResponse:")
        print(response.output_text)
    except Exception as ex:
        print(f"Request failed: {ex}")

    pause()


def get_weather_make_up_something(location: str) -> None:
    clear_console()
    print(f"""Example 2
 - Ask AI "What is the current weather in {location}?"
 - Model Direct (using the OpenAI Responses API against unified AI services endpoint)
 - Ask it to make something up because it doesn't have supporting data.""")

    # AI prep
    system_prompt = """You are a helpful weather assistant.
- I know you don't have supporting data, so just make something up.
- Keep it short."""
    user_prompt = f"What is the current weather today for {location}?"

    print_prompts(system_prompt, user_prompt)

    try:
        response = create_client().responses.create(
            model=DEPLOYMENT_NAME,
            instructions=system_prompt,
            input=[message_item("user", user_prompt)],
        )
        print("\nResponse:")
        print(response.output_text)
    except Exception as ex:
        print(f"Request failed: {ex}")

    pause()


def get_weather_json_in_string_out(location: str) -> None:
    clear_console()
    print(f"""Example 3
 - Ask AI "What is the current weather in {location}?"
 - Model Direct (using the OpenAI Responses API against unified AI services endpoint)
 - Provide raw JSON input from a weather API
 - String output from AI""")

    # Non-AI prep
    weather_data_json = get_weather_data_json(location)

    # AI prep
    system_prompt = """You are a helpful weather assistant.
Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.
GitHub-flavored Markdown is allowed when it makes the answer easier to read. Do not emit raw HTML.
Use one or two friendly sentences of the current weather and include the place name, temperature, wind speed, wind direction, and overall conditions. Keep those facts even if they also appear in JSON."""
    user_prompt = f"""You are given this WeatherConditions JSON:
{weather_data_json}

Describe today's current weather in {location}?"""

    print_prompts(system_prompt, user_prompt)

    try:
        response = create_client().responses.create(
            model=DEPLOYMENT_NAME,
            instructions=system_prompt,
            input=[message_item("user", user_prompt)],
        )
        print("\nResponse:")
        print(response.output_text)
    except Exception as ex:
        print(f"Request failed: {ex}")

    pause()


def get_weather_json_in_json_out(location: str) -> None:
    clear_console()
    print(f"""Example 4
 - Ask AI "What is the current weather in {location}?"
 - Model Direct (using the OpenAI Responses API against unified AI services endpoint)
 - Provide raw JSON input from a weather API
 - JSON output from AI""")

    # Non-AI prep
    weather_data_json = get_weather_data_json(location)

    # AI prep
    system_prompt = """You are a helpful weather assistant.
Use U.S. customary units only: °F, mph, and " (e.g. 72°F, 8 mph, 1"). Convert from the weather tool's native units (°C, km/h, mm). Do not present C, KPH, or MM in responses.

Return valid JSON with these fields:
- fullSummary (string) (one or two friendly sentences of the current weather including place name, temperature, wind speed, wind direction, and overall conditions — keep those facts even though some are also JSON fields; GitHub-flavored Markdown is allowed when it helps readability)
- temperatureF (number) in Fahrenheit
- windSpeedMPH (number) in MPH
- windDirectionSourceDegrees (integer): Copy current_weather.winddirection from the weather tool exactly (meteorological source direction — where the wind comes from). Normalize to 0–360 if needed. Do not add 180.
- windDirectionSource (string): 16-point compass label derived from windDirectionSourceDegrees. Round normalized degrees to the nearest 22.5° sector and map to one of: N, NNE, NE, ENE, E, ESE, SE, SSE, S, SSW, SW, WSW, W, WNW, NW, NNW (e.g. 180 → S, 224 → SW).
- conditions (string)
- latitude (number): Decimal degrees from the provided WeatherConditions JSON (positive north, negative south).
- longitude (number): Decimal degrees from the provided WeatherConditions JSON (positive east, negative west).

You only return valid JSON."""
    user_prompt = f"""You are given this WeatherConditions JSON:
{weather_data_json}

Use {location} as the location context."""

    print_prompts(system_prompt, user_prompt)
    print("\nAI Output Schema:")
    print(json.dumps(AI_OUTPUT_SCHEMA, indent=2))

    try:
        response = create_client().responses.create(
            model=DEPLOYMENT_NAME,
            instructions=system_prompt,
            input=[message_item("user", user_prompt)],
            text=json_schema_text_format(),
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


# ---------------------------------------------------------------------------
# AI helpers
# ---------------------------------------------------------------------------


def create_client() -> OpenAI:
    # The endpoint needs the /openai/v1 suffix; the SDK appends /responses.
    return OpenAI(base_url=ENDPOINT, api_key=get_api_key())


def get_api_key() -> str:
    api_key = os.environ.get("AZURE_FOUNDRY_PROD_KEY")
    if not api_key:
        raise RuntimeError("API key not found in environment variables.")
    return api_key


def message_item(role: str, text: str) -> dict[str, Any]:
    """An explicit Responses input message. The Foundry endpoint rejects role-only shorthand
    items without "type": "message" (400 invalid_value on input[n].type)."""
    return {"type": "message", "role": role, "content": text}


def json_schema_text_format() -> dict[str, Any]:
    return {
        "format": {
            "type": "json_schema",
            "name": "ai_weather_response",
            "schema": AI_OUTPUT_SCHEMA,
            "strict": True,
        }
    }


def parse_ai_weather(content: str | None) -> dict[str, Any] | None:
    """Parse the model's JSON and recompute the compass label from the normalized degrees,
    like the C# console does with WeatherUnitConversion."""
    if not content:
        return None
    ai_weather = json.loads(content)
    if not isinstance(ai_weather, dict):
        return None
    raw_degrees = ai_weather.get("windDirectionSourceDegrees")
    if isinstance(raw_degrees, bool) or not isinstance(raw_degrees, (int, float)):
        raise ValueError("windDirectionSourceDegrees must be a number.")
    # round() is banker's rounding, same as C#'s Math.Round.
    degrees = normalize_source_degrees(int(round(raw_degrees)))
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
# Non-AI helpers (stand-ins for Core's GetLatLongEvent / GetPublicWeatherCurrentEvent)
# ---------------------------------------------------------------------------


def get_weather_data_json(location: str) -> str:
    lat_long = get_lat_long(location)
    weather_data = get_public_weather_current(lat_long["latitude"], lat_long["longitude"])
    return json.dumps(weather_data, indent=2)


def _get_json(url: str) -> Any:
    """GET JSON, retrying throttling (429), server errors (5xx), and transport errors a few times
    with a growing pause; other 4xx fail immediately. Same policy as mcp-srv-python's geo tools."""
    with httpx.Client(
        timeout=30.0,
        headers={"Accept": "application/json", "User-Agent": USER_AGENT},
        follow_redirects=True,
    ) as client:
        for attempt in range(ATTEMPTS):
            last_attempt = attempt == ATTEMPTS - 1
            try:
                response = client.get(url)
            except httpx.TransportError:
                if last_attempt:
                    raise
            else:
                if not (response.status_code == 429 or response.status_code >= 500) or last_attempt:
                    response.raise_for_status()
                    return response.json()
            time.sleep(RETRY_DELAY_SECONDS * (attempt + 1))
    raise AssertionError("unreachable")


def location_queries(location: str) -> list[str]:
    """The location as given, then (for "City, ST") just the part before the first comma."""
    queries = [location]
    if "," in location:
        city = location.split(",")[0].strip()
        if city.casefold() != location.casefold():
            queries.append(city)
    return queries


def get_lat_long(location: str) -> dict[str, Any]:
    """Best Open-Meteo geocoding match for a location name.

    Each query variant is tried in turn; one that still fails after its retries doesn't stop the
    next variant. Re-raises the last error when no variant answered."""
    last_error: Exception | None = None
    any_answered = False
    for query in location_queries(location):
        params = {"name": query, "count": 1, "language": "en", "format": "json"}
        try:
            data = _get_json(f"{OPEN_METEO_GEOCODING_URL}?{urlencode(params)}")
        except (httpx.HTTPError, ValueError) as ex:
            last_error = ex
            continue

        any_answered = True
        matches = (data or {}).get("results") or []
        if matches:
            match = matches[0]
            return {
                "rank": 1,
                "name": match.get("name") or "",
                "state": match.get("admin1") or "",
                "country": match.get("country") or "",
                "latitude": match.get("latitude") or 0.0,
                "longitude": match.get("longitude") or 0.0,
            }

    if last_error is not None and not any_answered:
        raise last_error
    raise ValueError(f"Non-AI: No results found for '{location}'.")


def build_current_weather_url(latitude: float, longitude: float) -> str:
    params = {
        "latitude": latitude,
        "longitude": longitude,
        "current_weather": "true",
        "temperature_unit": "celsius",
        "wind_speed_unit": "kmh",
    }
    return f"{OPEN_METEO_FORECAST_URL}?{urlencode(params)}"


def get_public_weather_current(latitude: float, longitude: float) -> dict[str, Any]:
    weather_data = _get_json(build_current_weather_url(latitude, longitude))
    current = weather_data.get("current_weather") if isinstance(weather_data, dict) else None
    if isinstance(current, dict) and isinstance(current.get("winddirection"), (int, float)):
        # Core's NonAICurrentWeatherResponse models winddirection as an int; hand the model the same.
        current["winddirection"] = int(round(current["winddirection"]))
    return weather_data


# ---------------------------------------------------------------------------
# Console helpers
# ---------------------------------------------------------------------------


def clear_console() -> None:
    print("\033[2J\033[H", end="")


def print_prompts(system_prompt: str, user_prompt: str) -> None:
    print("\nSystem Prompt:")
    print(system_prompt)
    print("\nUser Prompt:")
    print(user_prompt)


def pause() -> None:
    input("\nPress Enter to continue.")


if __name__ == "__main__":
    main()
