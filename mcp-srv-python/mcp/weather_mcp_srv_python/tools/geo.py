"""GetLatLong (Open-Meteo geocoding) and GetLocation (Nominatim reverse geocoding)."""

import asyncio
from typing import Any
from urllib.parse import urlencode

import httpx

OPEN_METEO_GEOCODING_URL = "https://geocoding-api.open-meteo.com/v1/search"
NOMINATIM_REVERSE_URL = "https://nominatim.openstreetmap.org/reverse"

# Nominatim's usage policy requires an identifying User-Agent; same value as Core's GetLocationHandler.
USER_AGENT = "Weather-1116/1.0 (https://github.com/dustinkorzaan/weather-1116)"

DEFAULT_COUNT = 5
MAX_COUNT = 100
ATTEMPTS = 3
RETRY_DELAY_SECONDS = 0.5


def normalize_count(count: int | None) -> int:
    """Reset the match count into [1, 100] instead of failing; missing uses the default."""
    if count is None:
        return DEFAULT_COUNT
    return min(max(int(count), 1), MAX_COUNT)


def _new_client() -> httpx.AsyncClient:
    return httpx.AsyncClient(
        timeout=30.0,
        headers={"Accept": "application/json", "User-Agent": USER_AGENT},
        follow_redirects=True,
    )


async def _get_json(client: httpx.AsyncClient, url: str, retry_delay_seconds: float) -> Any:
    """GET JSON, retrying throttling (429), server errors (5xx), and transport errors a few times
    with a growing pause; other 4xx fail immediately."""
    for attempt in range(ATTEMPTS):
        last_attempt = attempt == ATTEMPTS - 1
        try:
            response = await client.get(url)
        except httpx.TransportError:
            if last_attempt:
                raise
        else:
            if not (response.status_code == 429 or response.status_code >= 500) or last_attempt:
                response.raise_for_status()
                return response.json()
        await asyncio.sleep(retry_delay_seconds * (attempt + 1))
    raise AssertionError("unreachable")


def build_geocoding_url(query: str, count: int) -> str:
    params = {"name": query, "count": count, "language": "en", "format": "json"}
    return f"{OPEN_METEO_GEOCODING_URL}?{urlencode(params)}"


def location_queries(location: str) -> list[str]:
    """The location as given, then (for inputs like "City, ST") just the part before the first comma,
    de-duplicated case-insensitively."""
    candidates = [location]
    if "," in location:
        candidates.append(location.split(",")[0].strip())

    queries: list[str] = []
    seen: set[str] = set()
    for query in candidates:
        key = query.casefold()
        if key not in seen:
            seen.add(key)
            queries.append(query)
    return queries


def _to_lat_long(rank: int, match: dict[str, Any]) -> dict[str, Any]:
    return {
        "rank": rank,
        "name": match.get("name") or "",
        "state": match.get("admin1") or "",
        "country": match.get("country") or "",
        "latitude": match.get("latitude") or 0.0,
        "longitude": match.get("longitude") or 0.0,
    }


async def get_lat_long(
    location: str,
    count: int | None = DEFAULT_COUNT,
    *,
    retry_delay_seconds: float = RETRY_DELAY_SECONDS,
) -> dict[str, Any]:
    """Ranked latitude/longitude matches for a location name (rank 1 is the best match), at most
    count of them. Raises ValueError when no query variant matches anything."""
    count = normalize_count(count)
    async with _new_client() as client:
        for query in location_queries(location):
            data = await _get_json(client, build_geocoding_url(query, count), retry_delay_seconds)
            matches = (data or {}).get("results") or []
            if matches:
                return {"results": [_to_lat_long(i + 1, m) for i, m in enumerate(matches[:count])]}

    raise ValueError(f"Non-AI: No results found for '{location}'.")


def build_reverse_geocode_url(latitude: float, longitude: float) -> str:
    params = {
        "lat": repr(float(latitude)),
        "lon": repr(float(longitude)),
        "format": "jsonv2",
        "addressdetails": 1,
        "zoom": 10,
        "accept-language": "en",
    }
    return f"{NOMINATIM_REVERSE_URL}?{urlencode(params)}"


def _clean(value: Any) -> str:
    return value.strip() if isinstance(value, str) else ""


def location_from_address(address: dict[str, Any] | None) -> str:
    """City, State in the US; City, State, Country elsewhere; empty when nothing useful is present."""
    if not address:
        return ""

    city = next(
        (
            cleaned
            for key in ("city", "town", "village", "municipality", "county")
            if (cleaned := _clean(address.get(key)))
        ),
        "",
    )
    state = _clean(address.get("state"))
    country = _clean(address.get("country"))
    is_us = _clean(address.get("country_code")).casefold() == "us"

    parts = [part for part in (city, state) if part]
    if not is_us and country:
        parts.append(country)
    return ", ".join(parts)


def format_coordinates(latitude: float, longitude: float) -> str:
    """e.g. 35.51° N, 86.58° W"""

    def hemisphere(value: float, positive: str, negative: str) -> str:
        return f"{abs(value):.2f}° {positive if value >= 0 else negative}"

    return f"{hemisphere(latitude, 'N', 'S')}, {hemisphere(longitude, 'E', 'W')}"


def location_from_reverse(data: dict[str, Any] | None, latitude: float, longitude: float) -> str:
    """Structured address label, then Nominatim's feature name, then a formatted coordinate."""
    data = data or {}
    structured = location_from_address(data.get("address"))
    if structured:
        return structured

    name = _clean(data.get("name"))
    if name:
        return name

    return format_coordinates(latitude, longitude)


async def get_location(
    latitude: float,
    longitude: float,
    *,
    retry_delay_seconds: float = RETRY_DELAY_SECONDS,
) -> dict[str, Any]:
    """Reverse-geocode a latitude/longitude to a simple place label."""
    async with _new_client() as client:
        data = await _get_json(client, build_reverse_geocode_url(latitude, longitude), retry_delay_seconds)
    return {"location": location_from_reverse(data if isinstance(data, dict) else None, latitude, longitude)}
