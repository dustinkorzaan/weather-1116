import math
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

from weather_mcp_srv_python.tools import cities
from weather_mcp_srv_python.tools.cities import (
    build_nearby_cities_url,
    get_cities,
    normalize_distance_km,
    normalize_size,
)


@pytest.mark.parametrize(
    ("value", "expected"),
    [(0, 1.0), (-50, 1.0), (5000, 1000.0), (math.inf, 1000.0), (math.nan, 161.0), (None, 161.0), (250.5, 250.5)],
)
def test_normalize_distance_km_resets_into_range(value, expected):
    assert normalize_distance_km(value) == expected


@pytest.mark.parametrize(("value", "expected"), [(-5, 0), (500, 100), (40, 40), (None, 25), (12.9, 12)])
def test_normalize_size_resets_into_range(value, expected):
    assert normalize_size(value) == expected


def test_build_nearby_cities_url_uses_escaped_iso6709_location_and_population_sort():
    url = build_nearby_cities_url(36.1627, -86.7816, 161, 10, 20)
    assert url.startswith(
        "http://geodb-free-service.wirefreethought.com/v1/geo/locations/%2B36.1627-086.7816/nearbyCities?"
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
async def test_get_cities_size_zero_returns_empty_without_calling_geodb(fake_geodb):
    fake = fake_geodb(50)
    result = await get_cities(36.16, -86.78, size=-3, page_delay_seconds=0)
    assert result["cities"] == []
    assert result["size"] == 0
    assert fake.requested_urls == []


@pytest.mark.asyncio
async def test_get_cities_resets_out_of_range_inputs_instead_of_failing(fake_geodb):
    fake = fake_geodb(500)
    result = await get_cities(36.16, -86.78, 20000, 1000, page_delay_seconds=0)
    assert result["distanceKm"] == 1000.0
    assert result["size"] == 100
    assert result["returned"] == 100
    assert all("radius=1000" in url for url in fake.requested_urls)


@pytest.mark.asyncio
async def test_get_cities_pages_ten_at_a_time_until_default_size(fake_geodb):
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
    result = await get_cities(36.16, -86.78, size=100, page_delay_seconds=0)
    assert result["returned"] == 12
    assert result["totalAvailable"] == 12
    assert len(fake.requested_urls) == 2


@pytest.mark.asyncio
async def test_get_cities_maps_geodb_fields(fake_geodb):
    fake_geodb(1)
    result = await get_cities(36.16, -86.78, size=1, page_delay_seconds=0)
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
                "params": {"name": "GetCities", "arguments": {"latitude": 36.16, "longitude": -86.78, "distanceKM": 300, "size": 12}},
            },
        )

    assert response.status_code == 200
    data_line = next((line for line in response.text.splitlines() if line.startswith("data: ")), None)
    payload = json.loads(data_line[len("data: "):] if data_line else response.text)
    assert not payload["result"].get("isError")
    result = json.loads(payload["result"]["content"][0]["text"])
    assert result["returned"] == 12
    assert result["distanceKm"] == 300
    assert all("radius=300" in url for url in fake.requested_urls)
