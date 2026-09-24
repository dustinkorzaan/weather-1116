import json
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

from weather_mcp_srv_python.tools import geo
from weather_mcp_srv_python.tools.geo import (
    build_geocoding_url,
    build_reverse_geocode_url,
    format_coordinates,
    get_lat_long,
    get_location,
    location_from_address,
    location_from_reverse,
    location_queries,
    normalize_count,
)


def _install_transport(monkeypatch, handler):
    real_client = httpx.AsyncClient

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(handler)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(geo.httpx, "AsyncClient", client_factory)


def _open_meteo_match(name, admin1="Tennessee", country="United States", latitude=36.16589, longitude=-86.78444):
    return {"name": name, "admin1": admin1, "country": country, "latitude": latitude, "longitude": longitude}


@pytest.mark.parametrize(("value", "expected"), [(None, 5), (0, 1), (-3, 1), (7, 7), (500, 100)])
def test_normalize_count_resets_into_range(value, expected):
    assert normalize_count(value) == expected


def test_location_queries_adds_city_only_variant_for_comma_input():
    assert location_queries("Nashville, TN") == ["Nashville, TN", "Nashville"]
    assert location_queries("Nashville") == ["Nashville"]
    assert location_queries("nashville,") == ["nashville,", "nashville"]


def test_build_geocoding_url_encodes_query():
    url = build_geocoding_url("Nashville, TN", 5)
    assert url.startswith("https://geocoding-api.open-meteo.com/v1/search?")
    query = parse_qs(urlparse(url).query)
    assert query == {"name": ["Nashville, TN"], "count": ["5"], "language": ["en"], "format": ["json"]}


def test_build_reverse_geocode_url_uses_plain_decimal_coordinates():
    url = build_reverse_geocode_url(36.1627, -86.7816)
    assert url.startswith("https://nominatim.openstreetmap.org/reverse?")
    query = parse_qs(urlparse(url).query)
    assert query["lat"] == ["36.1627"]
    assert query["lon"] == ["-86.7816"]
    assert query["zoom"] == ["10"]
    assert query["accept-language"] == ["en"]
    assert query["format"] == ["jsonv2"]


@pytest.mark.asyncio
async def test_get_lat_long_ranks_matches_and_maps_fields(monkeypatch):
    requested: list[str] = []

    def transport(request: httpx.Request) -> httpx.Response:
        requested.append(str(request.url))
        return httpx.Response(
            200,
            json={"results": [_open_meteo_match("Nashville"), _open_meteo_match("Nashville", "Georgia", latitude=31.2, longitude=-83.25)]},
        )

    _install_transport(monkeypatch, transport)
    result = await get_lat_long("Nashville", retry_delay_seconds=0)
    assert result == {
        "results": [
            {"rank": 1, "name": "Nashville", "state": "Tennessee", "country": "United States", "latitude": 36.16589, "longitude": -86.78444},
            {"rank": 2, "name": "Nashville", "state": "Georgia", "country": "United States", "latitude": 31.2, "longitude": -83.25},
        ]
    }
    assert len(requested) == 1


@pytest.mark.asyncio
async def test_get_lat_long_falls_back_to_city_only_query(monkeypatch):
    names: list[str] = []

    def transport(request: httpx.Request) -> httpx.Response:
        name = request.url.params["name"]
        names.append(name)
        if name == "Nashville, TN":
            return httpx.Response(200, json={"generationtime_ms": 0.1})
        return httpx.Response(200, json={"results": [_open_meteo_match("Nashville")]})

    _install_transport(monkeypatch, transport)
    result = await get_lat_long("Nashville, TN", retry_delay_seconds=0)
    assert names == ["Nashville, TN", "Nashville"]
    assert result["results"][0]["rank"] == 1


@pytest.mark.asyncio
async def test_get_lat_long_trims_to_count(monkeypatch):
    def transport(request: httpx.Request) -> httpx.Response:
        return httpx.Response(200, json={"results": [_open_meteo_match(f"Place {i}") for i in range(8)]})

    _install_transport(monkeypatch, transport)
    result = await get_lat_long("Place", count=3, retry_delay_seconds=0)
    assert [r["rank"] for r in result["results"]] == [1, 2, 3]


@pytest.mark.asyncio
async def test_get_lat_long_raises_when_nothing_matches(monkeypatch):
    _install_transport(monkeypatch, lambda request: httpx.Response(200, json={}))
    with pytest.raises(ValueError, match="No results found for 'Nowhere, ZZ'"):
        await get_lat_long("Nowhere, ZZ", retry_delay_seconds=0)


@pytest.mark.asyncio
async def test_get_lat_long_retries_5xx_then_succeeds(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        if calls["count"] == 1:
            return httpx.Response(503)
        return httpx.Response(200, json={"results": [_open_meteo_match("Nashville")]})

    _install_transport(monkeypatch, transport)
    result = await get_lat_long("Nashville", retry_delay_seconds=0)
    assert result["results"][0]["name"] == "Nashville"
    assert calls["count"] == 2


@pytest.mark.asyncio
async def test_get_lat_long_does_not_retry_400(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(400)

    _install_transport(monkeypatch, transport)
    with pytest.raises(httpx.HTTPStatusError):
        await get_lat_long("Nashville", retry_delay_seconds=0)
    assert calls["count"] == 1


@pytest.mark.asyncio
async def test_get_lat_long_falls_back_to_city_only_query_after_first_variant_fails(monkeypatch):
    names: list[str] = []

    def transport(request: httpx.Request) -> httpx.Response:
        name = request.url.params["name"]
        names.append(name)
        if name == "Nashville, TN":
            return httpx.Response(503)
        return httpx.Response(200, json={"results": [_open_meteo_match("Nashville")]})

    _install_transport(monkeypatch, transport)
    result = await get_lat_long("Nashville, TN", retry_delay_seconds=0)
    assert names == ["Nashville, TN"] * geo.ATTEMPTS + ["Nashville"]
    assert result["results"][0]["name"] == "Nashville"


@pytest.mark.asyncio
async def test_get_lat_long_reraises_when_every_variant_fails(monkeypatch):
    _install_transport(monkeypatch, lambda request: httpx.Response(503))
    with pytest.raises(httpx.HTTPStatusError):
        await get_lat_long("Nashville, TN", retry_delay_seconds=0)


@pytest.mark.asyncio
async def test_get_lat_long_reports_no_results_when_one_variant_answered_empty(monkeypatch):
    def transport(request: httpx.Request) -> httpx.Response:
        if request.url.params["name"] == "Nowhere, ZZ":
            return httpx.Response(503)
        return httpx.Response(200, json={})

    _install_transport(monkeypatch, transport)
    with pytest.raises(ValueError, match="No results found"):
        await get_lat_long("Nowhere, ZZ", retry_delay_seconds=0)


@pytest.mark.asyncio
async def test_get_lat_long_caches_successful_results(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(200, json={"results": [_open_meteo_match("Nashville")]})

    _install_transport(monkeypatch, transport)
    first = await get_lat_long("Nashville", retry_delay_seconds=0)
    second = await get_lat_long("Nashville", retry_delay_seconds=0)
    assert first == second
    assert calls["count"] == 1


@pytest.mark.asyncio
async def test_get_lat_long_does_not_cache_failures(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(200, json={})

    _install_transport(monkeypatch, transport)
    for _ in range(2):
        with pytest.raises(ValueError):
            await get_lat_long("Nowhere", retry_delay_seconds=0)
    assert calls["count"] == 2


@pytest.mark.asyncio
async def test_get_location_caches_by_coordinate(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(200, json={"address": {"city": "Nashville", "state": "Tennessee", "country_code": "us"}})

    _install_transport(monkeypatch, transport)
    await get_location(36.1627, -86.7816, retry_delay_seconds=0)
    await get_location(36.1627, -86.7816, retry_delay_seconds=0)
    await get_location(35.0, -86.0, retry_delay_seconds=0)
    assert calls["count"] == 2


@pytest.mark.asyncio
async def test_cache_entries_expire_after_ttl(monkeypatch):
    now = {"t": 1000.0}
    monkeypatch.setattr(geo.time, "monotonic", lambda: now["t"])
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(200, json={"name": "Somewhere"})

    _install_transport(monkeypatch, transport)
    await get_location(1.0, 2.0, retry_delay_seconds=0)
    now["t"] += geo.CACHE_TTL_SECONDS + 1
    await get_location(1.0, 2.0, retry_delay_seconds=0)
    assert calls["count"] == 2


def test_location_from_address_us_omits_country():
    address = {"city": "Nashville", "state": "Tennessee", "country": "United States", "country_code": "us"}
    assert location_from_address(address) == "Nashville, Tennessee"


def test_location_from_address_non_us_includes_country():
    address = {"town": " Banff ", "state": "Alberta", "country": "Canada", "country_code": "ca"}
    assert location_from_address(address) == "Banff, Alberta, Canada"


def test_location_from_address_prefers_city_then_town_then_village_then_municipality_then_county():
    assert location_from_address({"village": "V", "county": "C", "country_code": "us"}) == "V"
    assert location_from_address({"county": "Rutherford County", "state": "Tennessee", "country_code": "us"}) == "Rutherford County, Tennessee"
    assert location_from_address({"city": " ", "town": "T", "country_code": "us"}) == "T"


def test_location_from_address_empty_when_missing():
    assert location_from_address(None) == ""
    assert location_from_address({}) == ""


def test_format_coordinates_uses_hemispheres():
    assert format_coordinates(35.5123, -86.5812) == "35.51° N, 86.58° W"
    assert format_coordinates(-33.8688, 151.2093) == "33.87° S, 151.21° E"


def test_location_from_reverse_falls_back_to_name_then_coordinates():
    assert location_from_reverse({"name": " Pacific Ocean ", "address": {}}, 10, -140) == "Pacific Ocean"
    assert location_from_reverse({"error": "Unable to geocode"}, 10, -140) == "10.00° N, 140.00° W"
    assert location_from_reverse(None, 10, -140) == "10.00° N, 140.00° W"


@pytest.mark.asyncio
async def test_get_location_sends_user_agent_and_maps_address(monkeypatch):
    seen: dict[str, str] = {}

    def transport(request: httpx.Request) -> httpx.Response:
        seen["user_agent"] = request.headers["User-Agent"]
        return httpx.Response(
            200,
            json={"name": "Nashville", "address": {"city": "Nashville", "state": "Tennessee", "country": "United States", "country_code": "us"}},
        )

    _install_transport(monkeypatch, transport)
    result = await get_location(36.1627, -86.7816, retry_delay_seconds=0)
    assert result == {"location": "Nashville, Tennessee"}
    assert seen["user_agent"].startswith("Weather-1116/")


def _post_mcp(client, body):
    response = client.post(
        "/mcp",
        headers={
            "Authorization": "Bearer test-key",
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
        },
        json=body,
    )
    assert response.status_code == 200
    data_line = next((line for line in response.text.splitlines() if line.startswith("data: ")), None)
    return json.loads(data_line[len("data: "):] if data_line else response.text)


def test_get_lat_long_tool_call_end_to_end(monkeypatch):
    from starlette.testclient import TestClient

    _install_transport(monkeypatch, lambda request: httpx.Response(200, json={"results": [_open_meteo_match("Nashville")]}))
    monkeypatch.setenv("MCP_SRV_PYTHON_KEY", "test-key")
    from weather_mcp_srv_python.server import build_app

    with TestClient(build_app()) as client:
        payload = _post_mcp(
            client,
            {"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "GetLatLong", "arguments": {"location": "Nashville, TN"}}},
        )

    assert not payload["result"].get("isError")
    result = json.loads(payload["result"]["content"][0]["text"])
    assert result["results"][0] == {
        "rank": 1,
        "name": "Nashville",
        "state": "Tennessee",
        "country": "United States",
        "latitude": 36.16589,
        "longitude": -86.78444,
    }


def test_get_location_tool_call_end_to_end(monkeypatch):
    from starlette.testclient import TestClient

    _install_transport(
        monkeypatch,
        lambda request: httpx.Response(200, json={"address": {"city": "Paris", "state": "Ile-de-France", "country": "France", "country_code": "fr"}}),
    )
    monkeypatch.setenv("MCP_SRV_PYTHON_KEY", "test-key")
    from weather_mcp_srv_python.server import build_app

    with TestClient(build_app()) as client:
        payload = _post_mcp(
            client,
            {"jsonrpc": "2.0", "id": 1, "method": "tools/call", "params": {"name": "GetLocation", "arguments": {"latitude": 48.8566, "longitude": 2.3522}}},
        )

    assert not payload["result"].get("isError")
    assert json.loads(payload["result"]["content"][0]["text"]) == {"location": "Paris, Ile-de-France, France"}
