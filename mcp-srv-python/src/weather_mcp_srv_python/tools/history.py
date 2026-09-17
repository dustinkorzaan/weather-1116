"""GetPublicWeatherHistory: fetches recent past public weather from Open-Meteo."""

from typing import Any, Literal
from urllib.parse import urlencode

import httpx

OPEN_METEO_FORECAST_URL = "https://api.open-meteo.com/v1/forecast"

# Pin all series to metric units; the caller can convert as needed.
OPEN_METEO_UNITS = {
    "temperature_unit": "celsius",
    "wind_speed_unit": "kmh",
    "precipitation_unit": "mm",
}

# PascalCase to match the resolution values the MCP tool exposed when it lived on
# mcp-srv-app-service (Core.Weather.Events.PublicWeatherHistoryResolution), so callers/prompts
# tuned on the old tool keep working unchanged now that it's served by mcp-srv-python.
HistoryResolution = Literal["Daily", "Hourly"]

_RESOLUTION_QUERY: dict[str, dict[str, str]] = {
    "Daily": {
        "daily": "weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,"
        "wind_speed_10m_max,wind_direction_10m_dominant",
        "past_days": "7",
        "forecast_days": "0",
    },
    "Hourly": {
        "hourly": "temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m",
        "past_hours": "48",
        "forecast_hours": "0",
    },
}

# Open-Meteo can serialize a series field as JSON null instead of omitting it, so every
# key that could appear in each response block is filled in with an empty list when null.
_HOURLY_KEYS = ("time", "temperature_2m", "precipitation", "weather_code", "wind_speed_10m", "wind_direction_10m")
_DAILY_KEYS = (
    "time",
    "weather_code",
    "temperature_2m_max",
    "temperature_2m_min",
    "precipitation_sum",
    "wind_speed_10m_max",
    "wind_direction_10m_dominant",
)
_PRECIPITATION_KEYS = ("precipitation", "precipitation_sum")


def _build_history_url(latitude: float, longitude: float, resolution: HistoryResolution) -> str:
    if resolution not in _RESOLUTION_QUERY:
        raise ValueError(f"Unsupported history resolution: {resolution!r}")

    params = {
        "latitude": latitude,
        "longitude": longitude,
        **_RESOLUTION_QUERY[resolution],
        **OPEN_METEO_UNITS,
        "timezone": "auto",
    }
    return f"{OPEN_METEO_FORECAST_URL}?{urlencode(params)}"


def _normalize_series_block(block: dict[str, Any] | None, keys: tuple[str, ...]) -> None:
    """Fill in any null series with an empty list and clamp negative precipitation readings to zero."""
    if block is None:
        return

    for key in keys:
        if block.get(key) is None:
            block[key] = []

    for key in _PRECIPITATION_KEYS:
        values = block.get(key)
        if values:
            block[key] = [max(value, 0) for value in values]


async def get_public_weather_history(
    latitude: float,
    longitude: float,
    resolution: HistoryResolution = "Daily",
) -> dict[str, Any]:
    """Fetch recent past public weather for a latitude/longitude from Open-Meteo.

    Daily is the previous 7 days, Hourly is the previous 48 hours.
    """
    url = _build_history_url(latitude, longitude, resolution)

    async with httpx.AsyncClient(timeout=30.0) as client:
        response = await client.get(url)
        response.raise_for_status()
        weather_data: dict[str, Any] = response.json()

    _normalize_series_block(weather_data.get("hourly"), _HOURLY_KEYS)
    _normalize_series_block(weather_data.get("daily"), _DAILY_KEYS)

    return weather_data
