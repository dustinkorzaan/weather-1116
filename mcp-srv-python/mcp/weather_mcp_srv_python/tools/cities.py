"""GetCities: the largest cities (by population) near a latitude/longitude, from GeoDB Cities."""

import asyncio
import math
from typing import Any
from urllib.parse import quote, urlencode

import httpx

# GeoDB's free no-key service. It returns at most 10 results per request, allows about 1 request
# per second (so pages are fetched sequentially with a short pause), and rejects (403) any radius
# above 100 of the requested unit.
GEODB_BASE_URL = "https://geodb-free-service.wirefreethought.com/v1/geo"
GEODB_PAGE_LIMIT = 10
GEODB_MAX_RADIUS_KM = 100.0
PAGE_DELAY_SECONDS = 1.1

MIN_DISTANCE_KM = 1.0
DEFAULT_DISTANCE_KM = 161.0
MAX_DISTANCE_KM = 1000.0
MIN_SIZE = 0
DEFAULT_SIZE = 25
MAX_SIZE = 100


def normalize_distance_km(distance_km: float | None) -> float:
    """Reset the radius into [1, 1000] km instead of failing; missing/NaN uses the default."""
    if distance_km is None or math.isnan(distance_km):
        return DEFAULT_DISTANCE_KM
    return min(max(float(distance_km), MIN_DISTANCE_KM), MAX_DISTANCE_KM)


def normalize_size(size: int | float | None) -> int:
    """Reset the result count into [0, 100] instead of failing; missing uses the default."""
    if size is None or (isinstance(size, float) and math.isnan(size)):
        return DEFAULT_SIZE
    if isinstance(size, float) and math.isinf(size):
        return MAX_SIZE if size > 0 else MIN_SIZE
    return min(max(int(size), MIN_SIZE), MAX_SIZE)


def _format_number(value: float) -> str:
    return f"{value:.3f}".rstrip("0").rstrip(".")


def build_nearby_cities_url(latitude: float, longitude: float, distance_km: float, limit: int, offset: int) -> str:
    """GeoDB location ids are ISO-6709 (e.g. +36.1627-086.7816); the leading '+' must be escaped."""
    location_id = f"{latitude:+08.4f}{longitude:+09.4f}"
    params = {
        "radius": _format_number(distance_km),
        "distanceUnit": "KM",
        "types": "CITY",
        "sort": "-population",
        "limit": limit,
        "offset": offset,
    }
    return f"{GEODB_BASE_URL}/locations/{quote(location_id, safe='')}/nearbyCities?{urlencode(params)}"


def _to_city(city: dict[str, Any]) -> dict[str, Any]:
    return {
        "name": city.get("name") or city.get("city") or "",
        "region": city.get("region"),
        "country": city.get("country"),
        "latitude": city.get("latitude"),
        "longitude": city.get("longitude"),
        "distanceKm": city.get("distance") or 0,
        "population": city.get("population"),
    }


async def get_cities(
    latitude: float,
    longitude: float,
    distance_km: float | None = DEFAULT_DISTANCE_KM,
    size: int | None = DEFAULT_SIZE,
    *,
    page_delay_seconds: float = PAGE_DELAY_SECONDS,
) -> dict[str, Any]:
    """Largest cities within distance_km of a latitude/longitude, largest population first.

    distance_km is reset into [1, 1000] and size into [0, 100] on every call; size 0 returns an
    empty list without calling GeoDB.
    """
    distance_km = min(normalize_distance_km(distance_km), GEODB_MAX_RADIUS_KM)
    size = normalize_size(size)
    result: dict[str, Any] = {
        "distanceKm": distance_km,
        "size": size,
        "returned": 0,
        "totalAvailable": 0,
        "cities": [],
    }
    if size == 0:
        return result

    cities: list[dict[str, Any]] = result["cities"]
    offset = 0
    async with httpx.AsyncClient(timeout=30.0, headers={"Accept": "application/json"}, follow_redirects=True) as client:
        while len(cities) < size:
            if offset > 0:
                await asyncio.sleep(page_delay_seconds)

            limit = min(GEODB_PAGE_LIMIT, size - len(cities))
            response = await client.get(build_nearby_cities_url(latitude, longitude, distance_km, limit, offset))
            response.raise_for_status()
            page: dict[str, Any] = response.json()

            data = page.get("data") or []
            result["totalAvailable"] = (page.get("metadata") or {}).get("totalCount", result["totalAvailable"])
            cities.extend(_to_city(city) for city in data)
            offset += len(data)

            if not data or offset >= result["totalAvailable"]:
                break

    result["returned"] = len(cities)
    return result
