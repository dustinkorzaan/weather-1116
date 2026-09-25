import json
from types import SimpleNamespace
from urllib.parse import parse_qs, urlparse

import httpx
import pytest

import foundry_console_v3 as app


def test_tools_are_strict_and_require_every_property():
    names = [tool["name"] for tool in app.TOOLS]

    assert names == ["GetLatLong", "GetPublicWeatherCurrent"]
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
    assert second_input[0] == {"type": "message", "role": "user", "content": "user"}
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


@pytest.mark.parametrize("name", ["GetLocation", "GetPublicWeatherForecast", "GetPublicWeatherHistory", "GetCities"])
def test_call_tool_unknown_raises(name):
    with pytest.raises(NotImplementedError, match=f"Unexpected tool call: {name}"):
        app.call_tool(name, "{}")


@pytest.mark.parametrize(("degrees", "expected"), [(0, "N"), (180, "S"), (224, "SW"), (340, "NNW"), (349, "N")])
def test_degrees_to_compass(degrees, expected):
    assert app.degrees_to_compass(degrees) == expected


def test_parse_ai_weather_recomputes_compass():
    result = app.parse_ai_weather(json.dumps({"windDirectionSourceDegrees": 540, "windDirectionSource": "N"}))

    assert result["windDirectionSourceDegrees"] == 180
    assert result["windDirectionSource"] == "S"


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


def test_build_current_weather_url():
    query = parse_qs(urlparse(app.build_current_weather_url(36.16, -86.78)).query)

    assert query == {
        "latitude": ["36.16"],
        "longitude": ["-86.78"],
        "current_weather": ["true"],
        "temperature_unit": ["celsius"],
        "wind_speed_unit": ["kmh"],
    }


def test_get_public_weather_current_dispatch(monkeypatch):
    monkeypatch.setattr(app, "get_public_weather_current", lambda lat, lon: {"current_weather": {"latitude": lat, "longitude": lon}})

    output = app.call_tool("GetPublicWeatherCurrent", '{"latitude": 36.16, "longitude": -86.78}')

    assert json.loads(output) == {"current_weather": {"latitude": 36.16, "longitude": -86.78}}


def _mock_httpx(monkeypatch, handler):
    real_client = httpx.Client

    def client_factory(*args, **kwargs):
        kwargs["transport"] = httpx.MockTransport(handler)
        return real_client(*args, **kwargs)

    monkeypatch.setattr(app.httpx, "Client", client_factory)
