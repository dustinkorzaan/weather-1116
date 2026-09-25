import json
from types import SimpleNamespace

import pytest

import foundry_console_v4 as app

MCP_KEYS = {
    "MCP_SRV_FUNC_APP_KEY": "func-key",
    "MCP_SRV_APP_SERVICE_KEY": "app-service-key",
    "MCP_SRV_PYTHON_KEY": "python-key",
    "MCP_SRV_NODE_KEY": "node-key",
}


@pytest.fixture
def mcp_keys(monkeypatch):
    for name, value in MCP_KEYS.items():
        monkeypatch.setenv(name, value)


def test_build_mcp_tools(mcp_keys):
    tools = app.build_mcp_tools()

    assert [tool["server_label"] for tool in tools] == ["McpSrvFuncApp", "McpSrvAppService", "McpSrvPython", "McpSrvNode"]
    assert all(tool["type"] == "mcp" and tool["require_approval"] == "never" for tool in tools)
    assert tools[0]["server_url"].endswith("/runtime/webhooks/mcp")
    assert tools[0]["headers"] == {"x-functions-key": "func-key"}
    assert tools[1]["headers"] == {"Authorization": "Bearer app-service-key"}
    assert tools[2]["headers"] == {"Authorization": "Bearer python-key"}
    assert tools[3]["headers"] == {"Authorization": "Bearer node-key"}


def test_build_mcp_tools_missing_key_raises(mcp_keys, monkeypatch):
    monkeypatch.delenv("MCP_SRV_NODE_KEY")

    with pytest.raises(RuntimeError, match="MCP_SRV_NODE_KEY not found"):
        app.build_mcp_tools()


def test_get_weather_with_mcp_tools_single_call(mcp_keys, monkeypatch, capsys):
    monkeypatch.setenv("AZURE_FOUNDRY_PROD_KEY", "foundry-key")
    monkeypatch.setattr(app, "pause", lambda: None)
    monkeypatch.setattr(app, "clear_console", lambda: None)

    calls = []

    class FakeOpenAI:
        def __init__(self, base_url, api_key):
            assert base_url == app.ENDPOINT
            assert api_key == "foundry-key"
            self.responses = self

        def create(self, **kwargs):
            calls.append(kwargs)
            return SimpleNamespace(output_text=json.dumps({"fullSummary": "Sunny", "windDirectionSourceDegrees": 224}))

    monkeypatch.setattr(app, "OpenAI", FakeOpenAI)

    app.get_weather_with_mcp_tools("Nashville, TN")

    assert len(calls) == 1
    assert [item["role"] for item in calls[0]["input"]] == ["system", "user"]
    assert all(item["type"] == "message" for item in calls[0]["input"])
    assert len(calls[0]["tools"]) == 4
    assert calls[0]["text"]["format"]["strict"] is True
    out = capsys.readouterr().out
    assert '"windDirectionSource": "SW"' in out
    assert "Request failed" not in out


@pytest.mark.parametrize(("degrees", "expected"), [(0, "N"), (180, "S"), (224, "SW"), (340, "NNW"), (349, "N")])
def test_degrees_to_compass(degrees, expected):
    assert app.degrees_to_compass(degrees) == expected
