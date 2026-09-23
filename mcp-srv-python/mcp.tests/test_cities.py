import math
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

from weather_mcp_srv_python.tools import cities
from weather_mcp_srv_python.tools.cities import (
    build_nearby_cities_url,
    get_cities,
    normalize_max_cities,
    normalize_min_population,
    normalize_radius_km,
)


@pytest.mark.parametrize(
    ("value", "expected"),
    [(0, 1.0), (-50, 1.0), (5000, 1000.0), (math.inf, 1000.0), (math.nan, 161.0), (None, 161.0), (250.5, 250.5)],
)
def test_normalize_radius_km_resets_into_range(value, expected):
    assert normalize_radius_km(value) == expected


@pytest.mark.parametrize(("value", "expected"), [(-5, 0), (500, 100), (40, 40), (None, 25), (12.9, 12)])
def test_normalize_max_cities_resets_into_range(value, expected):
    assert normalize_max_cities(value) == expected


@pytest.mark.parametrize(("value", "expected"), [(-5, 0), (0, 0), (None, 0), (50000, 50000), (-math.inf, 0)])
def test_normalize_min_population_resets_negative_to_zero(value, expected):
    assert normalize_min_population(value) == expected


def test_build_nearby_cities_url_adds_min_population_only_when_positive():
    assert "minPopulation" not in build_nearby_cities_url(36.1627, -86.7816, 100, 0, 10, 0)
    assert "minPopulation=50000" in build_nearby_cities_url(36.1627, -86.7816, 100, 50000, 10, 0)


def test_build_nearby_cities_url_uses_escaped_iso6709_location_and_population_sort():
    url = build_nearby_cities_url(36.1627, -86.7816, 161, 0, 10, 20)
    assert url.startswith(
        "https://geodb-free-service.wirefreethought.com/v1/geo/locations/%2B36.1627-086.7816/nearbyCities?"
    )
    query = parse_qs(urlparse(url).query)
    assert query["radius"] == ["161"]
    assert query["distanceUnit"] == ["KM"]
    assert query["types"] == ["CITY"]
    assert query["sort"] == ["-population"]
    assert query["limit"] == ["10"]
    assert query["offset"] == ["20"]


class _FakeGeoDb:
    """Serves GeoDB-shaped pages from a fixed-size result set, honoring limit/offset."""

    def __init__(self, total_count: int):
        self.total_count = total_count
        self.requested_urls: list[str] = []

    def __call__(self, request: httpx.Request) -> httpx.Response:
        self.requested_urls.append(str(request.url))
        limit = int(request.url.params["limit"])
        offset = int(request.url.params["offset"])
        count = max(0, min(limit, self.total_count - offset))
        data = [
            {
                "name": f"City {i}",
                "region": "Tennessee",
                "country": "United States of America",
                "latitude": 36.1,
                "longitude": -86.7,
                "distance": 12.5,
                "population": 100000 - i,
            }
            for i in range(offset, offset + count)
        ]
        return httpx.Response(200, json={"data": data, "metadata": {"currentOffset": offset, "totalCount": self.total_count}})


@pytest.fixture
def fake_geodb(monkeypatch):
    def install(total_count: int) -> _FakeGeoDb:
        fake = _FakeGeoDb(total_count)
        real_client = httpx.AsyncClient

        def client_factory(*args, **kwargs):
            kwargs["transport"] = httpx.MockTransport(fake)
            return real_client(*args, **kwargs)

        monkeypatch.setattr(cities.httpx, "AsyncClient", client_factory)
        return fake

    return install


@pytest.mark.asyncio
async def test_get_cities_max_cities_zero_returns_empty_without_calling_geodb(fake_geodb):
    fake = fake_geodb(50)
    result = await get_cities(36.16, -86.78, max_cities=-3, page_delay_seconds=0)
    assert result["cities"] == []
    assert result["maxCities"] == 0
    assert fake.requested_urls == []


@pytest.mark.asyncio
async def test_get_cities_resets_out_of_range_inputs_instead_of_failing(fake_geodb):
    fake = fake_geodb(500)
    result = await get_cities(36.16, -86.78, 20000, -10, 1000, page_delay_seconds=0)
    assert result["radiusKm"] == 100.0
    assert result["minPopulation"] == 0
    assert result["maxCities"] == 100
    assert result["returned"] == 100
    assert all("radius=100&" in url for url in fake.requested_urls)


@pytest.mark.asyncio
@pytest.mark.parametrize(("requested", "sent"), [(161, 100.0), (1000, 100.0), (50, 50.0)])
async def test_get_cities_caps_radius_at_geodb_free_tier_maximum(fake_geodb, requested, sent):
    fake = fake_geodb(5)
    result = await get_cities(36.16, -86.78, requested, page_delay_seconds=0)
    assert result["radiusKm"] == sent
    assert f"radius={int(sent)}&" in fake.requested_urls[0]


@pytest.mark.asyncio
async def test_get_cities_follows_redirects(monkeypatch):
    fake = _FakeGeoDb(3)
    https_prefix = "https://geodb-free-service.wirefreethought.com"

    def transport(request: httpx.Request) -> httpx.Response:
        if str(request.url).startswith(https_prefix):
            return fake(request)
        return httpx.Response(308, headers={"Location": https_prefix + request.url.raw_path.decode()})

    real_client = httpx.AsyncClient

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(transport)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(cities.httpx, "AsyncClient", client_factory)
    monkeypatch.setattr(cities, "GEODB_BASE_URL", "http://geodb-free-service.wirefreethought.com/v1/geo")
    result = await get_cities(36.16, -86.78, 50, 0, 3, page_delay_seconds=0)
    assert result["returned"] == 3


@pytest.mark.asyncio
async def test_get_cities_pages_ten_at_a_time_until_default_max_cities(fake_geodb):
    fake = fake_geodb(500)
    result = await get_cities(36.16, -86.78, page_delay_seconds=0)
    assert result["returned"] == 25
    assert result["totalAvailable"] == 500
    assert len(fake.requested_urls) == 3
    assert "limit=10&offset=0" in fake.requested_urls[0]
    assert "limit=10&offset=10" in fake.requested_urls[1]
    assert "limit=5&offset=20" in fake.requested_urls[2]


@pytest.mark.asyncio
async def test_get_cities_stops_when_geodb_runs_out(fake_geodb):
    fake = fake_geodb(12)
    result = await get_cities(36.16, -86.78, max_cities=100, page_delay_seconds=0)
    assert result["returned"] == 12
    assert result["totalAvailable"] == 12
    assert len(fake.requested_urls) == 2


@pytest.mark.asyncio
async def test_get_cities_maps_geodb_fields(fake_geodb):
    fake_geodb(1)
    result = await get_cities(36.16, -86.78, max_cities=1, page_delay_seconds=0)
    assert result["cities"] == [
        {
            "name": "City 0",
            "region": "Tennessee",
            "country": "United States of America",
            "latitude": 36.1,
            "longitude": -86.7,
            "distanceKm": 12.5,
            "population": 100000,
        }
    ]


def test_get_cities_tool_call_end_to_end(monkeypatch, fake_geodb):
    import json

    from starlette.testclient import TestClient

    monkeypatch.setattr(cities, "PAGE_DELAY_SECONDS", 0)
    monkeypatch.setattr(cities.get_cities, "__kwdefaults__", {"page_delay_seconds": 0})
    fake = fake_geodb(500)
    monkeypatch.setenv("MCP_SRV_PYTHON_KEY", "test-key")
    from weather_mcp_srv_python.server import build_app

    with TestClient(build_app()) as client:
        response = client.post(
            "/mcp",
            headers={
                "Authorization": "Bearer test-key",
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
            },
            json={
                "jsonrpc": "2.0",
                "id": 1,
                "method": "tools/call",
                "params": {"name": "GetCities", "arguments": {"latitude": 36.16, "longitude": -86.78, "radiusKm": 300, "minPopulation": 50000, "maxCities": 12}},
            },
        )

    assert response.status_code == 200
    data_line = next((line for line in response.text.splitlines() if line.startswith("data: ")), None)
    payload = json.loads(data_line[len("data: "):] if data_line else response.text)
    assert not payload["result"].get("isError")
    result = json.loads(payload["result"]["content"][0]["text"])
    assert result["returned"] == 12
    assert result["radiusKm"] == 100
    assert result["minPopulation"] == 50000
    assert all("radius=100&" in url and "minPopulation=50000" in url for url in fake.requested_urls)


def _install_transport(monkeypatch, handler):
    real_client = httpx.AsyncClient

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(handler)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(cities.httpx, "AsyncClient", client_factory)


@pytest.mark.asyncio
async def test_get_cities_retries_429_then_succeeds(monkeypatch):
    fake = _FakeGeoDb(3)
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        if calls["count"] == 1:
            return httpx.Response(429)
        return fake(request)

    _install_transport(monkeypatch, transport)
    result = await get_cities(36.16, -86.78, 50, 0, 3, page_delay_seconds=0)
    assert result["returned"] == 3
    assert calls["count"] == 2


@pytest.mark.asyncio
async def test_get_cities_does_not_retry_403(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(403)

    _install_transport(monkeypatch, transport)
    with pytest.raises(httpx.HTTPStatusError):
        await get_cities(36.16, -86.78, 50, 0, 3, page_delay_seconds=0)
    assert calls["count"] == 1


@pytest.mark.asyncio
async def test_get_cities_gives_up_after_repeated_429(monkeypatch):
    calls = {"count": 0}

    def transport(request: httpx.Request) -> httpx.Response:
        calls["count"] += 1
        return httpx.Response(429)

    _install_transport(monkeypatch, transport)
    with pytest.raises(httpx.HTTPStatusError):
        await get_cities(36.16, -86.78, 50, 0, 3, page_delay_seconds=0)
    assert calls["count"] == cities.PAGE_ATTEMPTS
