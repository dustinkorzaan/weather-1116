import json
from types import SimpleNamespace
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

import foundry_console_v3 as app


def test_tools_are_strict_and_require_every_property():
    names = [tool["name"] for tool in app.TOOLS]

    assert names == [
        "GetLatLong", "GetLocation", "GetPublicWeatherCurrent", "GetPublicWeatherForecast", "GetPublicWeatherHistory",
    ]
    assert set(names) == set(app.TOOL_HANDLERS)
    for tool in app.TOOLS:
        assert tool["strict"] is True
        assert tool["parameters"]["additionalProperties"] is False
        assert tool["parameters"]["required"] == list(tool["parameters"]["properties"])


def test_run_tool_loop_runs_calls_then_returns_text(monkeypatch):
    monkeypatch.setitem(app.TOOL_HANDLERS, "GetLatLong", lambda args: {"results": [{"rank": 1, "location": args["location"]}]})

    function_call = SimpleNamespace(type="function_call", name="GetLatLong", arguments='{"location": "Nashville, TN"}', call_id="call_1")
    responses = [
        SimpleNamespace(output=[function_call], output_text=""),
        SimpleNamespace(output=[SimpleNamespace(type="message")], output_text='{"ok": true}'),
    ]
    calls = []

    class FakeResponses:
        def create(self, **kwargs):
            calls.append({**kwargs, "input": list(kwargs["input"])})
            return responses[len(calls) - 1]

    result = app.run_tool_loop(SimpleNamespace(responses=FakeResponses()), "system", "user")

    assert result == '{"ok": true}'
    assert len(calls) == 2
    assert calls[0]["tools"] == app.TOOLS
    assert calls[0]["text"]["format"]["strict"] is True
    second_input = calls[1]["input"]
    assert second_input[0] == {"role": "user", "content": "user"}
    assert second_input[1] is function_call
    assert second_input[2]["type"] == "function_call_output"
    assert second_input[2]["call_id"] == "call_1"
    assert json.loads(second_input[2]["output"]) == {"results": [{"rank": 1, "location": "Nashville, TN"}]}


def test_run_tool_loop_without_content_raises():
    class FakeResponses:
        def create(self, **kwargs):
            return SimpleNamespace(output=[], output_text="")

    with pytest.raises(RuntimeError, match="without producing content"):
        app.run_tool_loop(SimpleNamespace(responses=FakeResponses()), "system", "user")


def test_call_tool_unknown_raises():
    with pytest.raises(NotImplementedError, match="Unexpected tool call: GetCities"):
        app.call_tool("GetCities", "{}")


def test_forecast_and_history_default_to_daily(monkeypatch):
    seen = []
    monkeypatch.setattr(app, "get_public_weather_forecast", lambda lat, lon, res: seen.append(("forecast", res)) or {})
    monkeypatch.setattr(app, "get_public_weather_history", lambda lat, lon, res: seen.append(("history", res)) or {})

    app.TOOL_HANDLERS["GetPublicWeatherForecast"]({"latitude": 1, "longitude": 2, "resolution": None})
    app.TOOL_HANDLERS["GetPublicWeatherHistory"]({"latitude": 1, "longitude": 2, "resolution": "Hourly"})

    assert seen == [("forecast", "Daily"), ("history", "Hourly")]


@pytest.mark.parametrize(("degrees", "expected"), [(0, "N"), (180, "S"), (224, "SW"), (340, "NNW"), (349, "N")])
def test_degrees_to_compass(degrees, expected):
    assert app.degrees_to_compass(degrees) == expected


def test_parse_ai_weather_recomputes_compass():
    result = app.parse_ai_weather(json.dumps({"windDirectionSourceDegrees": 540, "windDirectionSource": "N"}))

    assert result["windDirectionSourceDegrees"] == 180
    assert result["windDirectionSource"] == "S"


def test_location_from_address():
    assert app.location_from_address({"city": "Nashville", "state": "Tennessee", "country": "United States", "country_code": "us"}) == "Nashville, Tennessee"
    assert app.location_from_address({"town": "Banff", "state": "Alberta", "country": "Canada", "country_code": "ca"}) == "Banff, Alberta, Canada"
    assert app.location_from_address(None) == ""


def test_format_coordinates():
    assert app.format_coordinates(35.514, -86.581) == "35.51° N, 86.58° W"
    assert app.format_coordinates(-33.9, 151.2) == "33.90° S, 151.20° E"


def test_get_location_falls_back_to_name_then_coordinates(monkeypatch):
    payloads = iter([{"name": "Lake Placid"}, {}])
    monkeypatch.setattr(app, "_get_json", lambda url: next(payloads))

    assert app.get_location(44.28, -73.98) == {"location": "Lake Placid"}
    assert app.get_location(44.28, -73.98) == {"location": "44.28° N, 73.98° W"}


def test_get_lat_long_ranks_matches(monkeypatch):
    def handler(request: httpx.Request) -> httpx.Response:
        assert request.headers["User-Agent"] == app.USER_AGENT
        assert request.url.params["count"] == "5"
        return httpx.Response(200, json={"results": [
            {"name": "Nashville", "admin1": "Tennessee", "country": "United States", "latitude": 36.16, "longitude": -86.78},
            {"name": "Nashville", "admin1": "Georgia", "country": "United States", "latitude": 31.2, "longitude": -83.25},
        ]})

    _mock_httpx(monkeypatch, handler)

    result = app.get_lat_long("Nashville, TN")

    assert [match["rank"] for match in result["results"]] == [1, 2]
    assert result["results"][1]["state"] == "Georgia"


def test_build_series_url_forecast_hourly():
    url = app.build_series_url(36.16, -86.78, app.FORECAST_QUERY["Hourly"])
    query = parse_qs(urlparse(url).query)

    assert query["hourly"] == [app.SUB_HOURLY_FIELDS]
    assert query["forecast_hours"] == ["48"]
    assert query["timezone"] == ["auto"]
    assert query["precipitation_unit"] == ["mm"]


def test_get_public_weather_history_normalizes_series(monkeypatch):
    monkeypatch.setattr(app, "_get_json", lambda url: {"daily": {"time": ["2026-09-24"], "precipitation_sum": [-0.1], "weather_code": None}})

    result = app.get_public_weather_history(36.16, -86.78, "Daily")

    assert result["daily"]["precipitation_sum"] == [0]
    assert result["daily"]["weather_code"] == []
    assert result["daily"]["temperature_2m_max"] == []


def test_unsupported_resolution_raises():
    with pytest.raises(ValueError, match="Unsupported history resolution: 'FifteenMinutes'"):
        app.get_public_weather_history(1, 2, "FifteenMinutes")


def _mock_httpx(monkeypatch, handler):
    real_client = httpx.Client

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(handler)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(app.httpx, "Client", client_factory)
