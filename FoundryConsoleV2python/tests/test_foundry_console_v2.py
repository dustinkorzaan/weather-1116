import json
from types import SimpleNamespace
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

import foundry_console_v2 as app


@pytest.mark.parametrize(
    ("degrees", "expected"),
    [(0, "N"), (11, "N"), (12, "NNE"), (180, "S"), (224, "SW"), (340, "NNW"), (349, "N"), (360, "N")],
)
def test_degrees_to_compass(degrees, expected):
    assert app.degrees_to_compass(degrees) == expected


@pytest.mark.parametrize(("degrees", "expected"), [(0, 0), (360, 0), (-90, 270), (725, 5)])
def test_normalize_source_degrees(degrees, expected):
    assert app.normalize_source_degrees(degrees) == expected


def test_parse_ai_weather_recomputes_compass():
    content = json.dumps({"fullSummary": "x", "windDirectionSourceDegrees": -136, "windDirectionSource": "N"})

    result = app.parse_ai_weather(content)

    assert result["windDirectionSourceDegrees"] == 224
    assert result["windDirectionSource"] == "SW"


def test_parse_ai_weather_empty_returns_none():
    assert app.parse_ai_weather("") is None
    assert app.parse_ai_weather(None) is None


def test_location_queries_adds_city_only_variant():
    assert app.location_queries("Nashville, TN") == ["Nashville, TN", "Nashville"]
    assert app.location_queries("Nashville") == ["Nashville"]


def test_build_current_weather_url():
    query = parse_qs(urlparse(app.build_current_weather_url(36.16, -86.78)).query)

    assert query == {
        "latitude": ["36.16"],
        "longitude": ["-86.78"],
        "current_weather": ["true"],
        "temperature_unit": ["celsius"],
        "wind_speed_unit": ["kmh"],
    }


def test_get_lat_long_falls_back_to_city_only(monkeypatch):
    names = []

    def handler(request: httpx.Request) -> httpx.Response:
        name = request.url.params["name"]
        names.append(name)
        if name == "Nashville":
            return httpx.Response(200, json={"results": [
                {"name": "Nashville", "admin1": "Tennessee", "country": "United States", "latitude": 36.16, "longitude": -86.78},
            ]})
        return httpx.Response(200, json={})

    _mock_httpx(monkeypatch, handler)

    result = app.get_lat_long("Nashville, TN")

    assert names == ["Nashville, TN", "Nashville"]
    assert result == {
        "rank": 1, "name": "Nashville", "state": "Tennessee", "country": "United States",
        "latitude": 36.16, "longitude": -86.78,
    }


def test_get_lat_long_no_match_raises(monkeypatch):
    _mock_httpx(monkeypatch, lambda request: httpx.Response(200, json={}))

    with pytest.raises(ValueError, match="No results found"):
        app.get_lat_long("Nowhere")


def test_json_in_json_out_sends_strict_schema(monkeypatch):
    monkeypatch.setattr(app, "get_weather_data_json", lambda location: "{}")
    monkeypatch.setattr(app, "pause", lambda: None)
    monkeypatch.setattr(app, "clear_console", lambda: None)

    calls = []

    class FakeResponses:
        def create(self, **kwargs):
            calls.append(kwargs)
            return SimpleNamespace(output_text=json.dumps({"windDirectionSourceDegrees": 180}))

    monkeypatch.setattr(app, "create_client", lambda: SimpleNamespace(responses=FakeResponses()))

    app.get_weather_json_in_json_out("Nashville, TN")

    assert len(calls) == 1
    assert calls[0]["model"] == app.DEPLOYMENT_NAME
    [sent] = calls[0]["input"]
    assert sent["type"] == "message"
    assert sent["role"] == "user"
    assert "Use Nashville, TN as the location context." in sent["content"]
    assert calls[0]["text"]["format"]["strict"] is True
    assert calls[0]["text"]["format"]["schema"] == app.AI_OUTPUT_SCHEMA


def test_get_api_key_missing_raises(monkeypatch):
    monkeypatch.delenv("AZURE_FOUNDRY_PROD_KEY", raising=False)

    with pytest.raises(RuntimeError, match="API key not found"):
        app.get_api_key()


def test_parse_ai_weather_rounds_fractional_degrees():
    result = app.parse_ai_weather(json.dumps({"windDirectionSourceDegrees": 359.6}))

    assert result["windDirectionSourceDegrees"] == 0
    assert result["windDirectionSource"] == "N"


@pytest.mark.parametrize("payload", [{}, {"windDirectionSourceDegrees": None}, {"windDirectionSourceDegrees": "SW"}])
def test_parse_ai_weather_missing_degrees_raises(payload):
    with pytest.raises(ValueError, match="windDirectionSourceDegrees must be a number"):
        app.parse_ai_weather(json.dumps(payload))


def test_get_lat_long_falls_back_after_http_error(monkeypatch):
    monkeypatch.setattr(app, "RETRY_DELAY_SECONDS", 0)
    names = []

    def handler(request: httpx.Request) -> httpx.Response:
        name = request.url.params["name"]
        names.append(name)
        if name == "Nashville":
            return httpx.Response(200, json={"results": [{"name": "Nashville", "latitude": 36.16, "longitude": -86.78}]})
        return httpx.Response(503)

    _mock_httpx(monkeypatch, handler)

    result = app.get_lat_long("Nashville, TN")

    assert names == ["Nashville, TN"] * app.ATTEMPTS + ["Nashville"]
    assert result["name"] == "Nashville"


def test_get_lat_long_reraises_when_no_variant_answers(monkeypatch):
    monkeypatch.setattr(app, "RETRY_DELAY_SECONDS", 0)
    _mock_httpx(monkeypatch, lambda request: httpx.Response(503))

    with pytest.raises(httpx.HTTPStatusError):
        app.get_lat_long("Nashville, TN")


def test_get_json_retries_transient_then_succeeds(monkeypatch):
    monkeypatch.setattr(app, "RETRY_DELAY_SECONDS", 0)
    statuses = iter([503, 429, 200])
    _mock_httpx(monkeypatch, lambda request: httpx.Response(next(statuses), json={"ok": True}))

    assert app._get_json("https://example.test/") == {"ok": True}


def test_get_json_does_not_retry_client_errors(monkeypatch):
    calls = []
    _mock_httpx(monkeypatch, lambda request: calls.append(1) or httpx.Response(404))

    with pytest.raises(httpx.HTTPStatusError):
        app._get_json("https://example.test/")
    assert len(calls) == 1


def test_get_public_weather_current_coerces_wind_direction(monkeypatch):
    def handler(request: httpx.Request) -> httpx.Response:
        assert request.headers["User-Agent"] == app.USER_AGENT
        return httpx.Response(200, json={"current_weather": {"temperature": 20.5, "winddirection": 224.6}})

    _mock_httpx(monkeypatch, handler)

    result = app.get_public_weather_current(36.16, -86.78)

    assert result["current_weather"]["winddirection"] == 225
    assert isinstance(result["current_weather"]["winddirection"], int)


def _mock_httpx(monkeypatch, handler):
    real_client = httpx.Client

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(handler)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(app.httpx, "Client", client_factory)
