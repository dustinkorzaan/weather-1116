"""GetPublicWeatherForecast: fetches an upcoming public weather forecast from Open-Meteo."""

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

ForecastResolution = Literal["daily", "hourly", "fifteen_minutes"]

_RESOLUTION_QUERY: dict[str, dict[str, str]] = {
    "daily": {
        "daily": "weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,"
        "wind_speed_10m_max,wind_direction_10m_dominant",
        "forecast_days": "7",
    },
    "hourly": {
        "hourly": "temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m",
        "forecast_hours": "48",
    },
    "fifteen_minutes": {
        "minutely_15": "temperature_2m,precipitation,weather_code,wind_speed_10m,wind_direction_10m",
        "forecast_minutely_15": "192",
    },
}

# Open-Meteo can serialize a series field as JSON null instead of omitting it, so every
# key that could appear in each response block is filled in with an empty list when null.
_SUB_HOURLY_KEYS = ("time", "temperature_2m", "precipitation", "weather_code", "wind_speed_10m", "wind_direction_10m")
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


def _build_forecast_url(latitude: float, longitude: float, resolution: ForecastResolution) -> str:
    if resolution not in _RESOLUTION_QUERY:
        raise ValueError(f"Unsupported forecast resolution: {resolution!r}")

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


async def get_public_weather_forecast(
    latitude: float,
    longitude: float,
    resolution: ForecastResolution = "daily",
) -> dict[str, Any]:
    """Fetch an upcoming public weather forecast for a latitude/longitude from Open-Meteo.

    daily is the next 7 days, hourly is the next 48 hours, and fifteen_minutes is the
    next 48 hours in 15-minute steps.
    """
    url = _build_forecast_url(latitude, longitude, resolution)

    async with httpx.AsyncClient(timeout=30.0) as client:
        response = await client.get(url)
        response.raise_for_status()
        weather_data: dict[str, Any] = response.json()

    _normalize_series_block(weather_data.get("hourly"), _SUB_HOURLY_KEYS)
    _normalize_series_block(weather_data.get("daily"), _DAILY_KEYS)
    _normalize_series_block(weather_data.get("minutely_15"), _SUB_HOURLY_KEYS)

    return weather_data
