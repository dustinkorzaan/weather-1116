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
PAGE_ATTEMPTS = 3
PAGE_DELAY_SECONDS = 1.1

MIN_RADIUS_KM = 1.0
DEFAULT_RADIUS_KM = 161.0
MAX_RADIUS_KM = 1000.0
DEFAULT_MIN_POPULATION = 0
MIN_MAX_CITIES = 0
DEFAULT_MAX_CITIES = 25
MAX_MAX_CITIES = 100


def normalize_radius_km(radius_km: float | None) -> float:
    """Reset the radius into [1, 1000] km instead of failing; missing/NaN uses the default."""
    if radius_km is None or math.isnan(radius_km):
        return DEFAULT_RADIUS_KM
    return min(max(float(radius_km), MIN_RADIUS_KM), MAX_RADIUS_KM)


def normalize_min_population(min_population: int | float | None) -> int:
    """Reset the population floor to 0 or more instead of failing; missing uses the default."""
    if min_population is None or (isinstance(min_population, float) and math.isnan(min_population)):
        return DEFAULT_MIN_POPULATION
    if isinstance(min_population, float) and math.isinf(min_population):
        return 0 if min_population < 0 else 2**62
    return max(int(min_population), 0)


def normalize_max_cities(max_cities: int | float | None) -> int:
    """Reset the result count into [0, 100] instead of failing; missing uses the default."""
    if max_cities is None or (isinstance(max_cities, float) and math.isnan(max_cities)):
        return DEFAULT_MAX_CITIES
    if isinstance(max_cities, float) and math.isinf(max_cities):
        return MAX_MAX_CITIES if max_cities > 0 else MIN_MAX_CITIES
    return min(max(int(max_cities), MIN_MAX_CITIES), MAX_MAX_CITIES)


def _format_number(value: float) -> str:
    return f"{value:.3f}".rstrip("0").rstrip(".")


def build_nearby_cities_url(
    latitude: float, longitude: float, radius_km: float, min_population: int, limit: int, offset: int
) -> str:
    """GeoDB location ids are ISO-6709 (e.g. +36.1627-086.7816); the leading '+' must be escaped."""
    location_id = f"{latitude:+08.4f}{longitude:+09.4f}"
    params: dict[str, Any] = {
        "radius": _format_number(radius_km),
        "distanceUnit": "KM",
        "types": "CITY",
    }
    if min_population > 0:
        params["minPopulation"] = min_population
    params.update({"sort": "-population", "limit": limit, "offset": offset})
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


async def _get_page(client: httpx.AsyncClient, url: str, page_delay_seconds: float) -> dict[str, Any]:
    """GET one GeoDB page, retrying throttling (429), server errors (5xx), and transport errors a
    few times with a growing pause; other 4xx (e.g. 403 for a too-large radius) fail immediately."""
    for attempt in range(PAGE_ATTEMPTS):
        last_attempt = attempt == PAGE_ATTEMPTS - 1
        try:
            response = await client.get(url)
        except httpx.TransportError:
            if last_attempt:
                raise
        else:
            if not (response.status_code == 429 or response.status_code >= 500) or last_attempt:
                response.raise_for_status()
                return response.json()
        await asyncio.sleep(page_delay_seconds * (attempt + 1))
    raise AssertionError("unreachable")


async def get_cities(
    latitude: float,
    longitude: float,
    radius_km: float | None = DEFAULT_RADIUS_KM,
    min_population: int | None = DEFAULT_MIN_POPULATION,
    max_cities: int | None = DEFAULT_MAX_CITIES,
    *,
    page_delay_seconds: float = PAGE_DELAY_SECONDS,
) -> dict[str, Any]:
    """Largest cities within radius_km of a latitude/longitude with at least min_population people,
    largest population first, at most max_cities of them.

    radius_km is reset into [1, 1000] (then capped at GeoDB's 100 km free-tier limit), min_population
    to 0 or more, and max_cities into [0, 100] on every call; max_cities 0 returns an empty list
    without calling GeoDB.
    """
    radius_km = min(normalize_radius_km(radius_km), GEODB_MAX_RADIUS_KM)
    min_population = normalize_min_population(min_population)
    max_cities = normalize_max_cities(max_cities)
    result: dict[str, Any] = {
        "radiusKm": radius_km,
        "minPopulation": min_population,
        "maxCities": max_cities,
        "returned": 0,
        "totalAvailable": 0,
        "cities": [],
    }
    if max_cities == 0:
        return result

    cities: list[dict[str, Any]] = result["cities"]
    offset = 0
    async with httpx.AsyncClient(timeout=30.0, headers={"Accept": "application/json"}, follow_redirects=True) as client:
        while len(cities) < max_cities:
            if offset > 0:
                await asyncio.sleep(page_delay_seconds)

            limit = min(GEODB_PAGE_LIMIT, max_cities - len(cities))
            url = build_nearby_cities_url(latitude, longitude, radius_km, min_population, limit, offset)
            page = await _get_page(client, url, page_delay_seconds)

            data = page.get("data") or []
            result["totalAvailable"] = (page.get("metadata") or {}).get("totalCount", result["totalAvailable"])
            cities.extend(_to_city(city) for city in data)
            offset += len(data)

            if not data or offset >= result["totalAvailable"]:
                break

    result["returned"] = len(cities)
    return result
