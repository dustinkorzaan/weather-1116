import os

from starlette.testclient import TestClient


def _build_test_app(monkeypatch, token="test-key"):
    monkeypatch.setenv("MCP_SRV_PYTHON_KEY", token)
    from weather_mcp_srv_python.server import build_app

    return build_app()


def test_wake_is_unauthenticated(monkeypatch):
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    response = client.get("/Wake")
    assert response.status_code == 200


def test_mcp_requires_bearer_token(monkeypatch):
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    response = client.post("/mcp")
    assert response.status_code == 401


def test_mcp_rejects_wrong_token(monkeypatch):
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    response = client.post("/mcp", headers={"Authorization": "Bearer wrong-token"})
    assert response.status_code == 401


def test_mcp_accepts_non_localhost_host_header(monkeypatch):
    """Regression: ACA FQDN Host headers must not return 421 after bearer auth."""
    app = _build_test_app(monkeypatch)
    client = TestClient(app, raise_server_exceptions=False)
    response = client.post(
        "/mcp",
        headers={
            "Authorization": "Bearer test-key",
            "Host": "wx1116-prod-mcp-srv-python.example.azurecontainerapps.io",
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
        },
        json={"jsonrpc": "2.0", "id": 1, "method": "tools/list"},
    )
    assert response.status_code != 421
    assert response.status_code != 401


def test_tools_list_returns_only_get_cities(monkeypatch):
    """Forecast/history live on mcp-srv-node; this host serves GetCities only, with
    distanceKM/size optional so callers can omit them."""
    import json

    app = _build_test_app(monkeypatch)
    with TestClient(app) as client:
        response = client.post(
            "/mcp",
            headers={
                "Authorization": "Bearer test-key",
                "Content-Type": "application/json",
                "Accept": "application/json, text/event-stream",
            },
            json={"jsonrpc": "2.0", "id": 1, "method": "tools/list"},
        )
    assert response.status_code == 200
    data_line = next((line for line in response.text.splitlines() if line.startswith("data: ")), None)
    payload = json.loads(data_line[len("data: "):] if data_line else response.text)
    tools = payload["result"]["tools"]
    assert [tool["name"] for tool in tools] == ["GetCities"]
    schema = tools[0]["inputSchema"]
    assert sorted(schema["required"]) == ["latitude", "longitude"]
    assert {"distanceKM", "size"} <= set(schema["properties"])


def test_mcp_rejects_all_requests_when_key_unset(monkeypatch):
    monkeypatch.delenv("MCP_SRV_PYTHON_KEY", raising=False)
    from weather_mcp_srv_python.server import build_app

    app = build_app()
    client = TestClient(app)
    response = client.post("/mcp")
    assert response.status_code == 401


def test_about_is_unauthenticated_and_healthy_when_key_set(monkeypatch):
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    response = client.get("/About")
    assert response.status_code == 200

    body = response.json()
    assert body["name"] == "mcp-srv-python"
    assert body["isHealthy"] is True
    assert body["children"] == []


def test_about_reports_unhealthy_when_key_unset(monkeypatch):
    monkeypatch.delenv("MCP_SRV_PYTHON_KEY", raising=False)
    from weather_mcp_srv_python.server import build_app

    app = build_app()
    client = TestClient(app)
    response = client.get("/About")
    assert response.status_code == 200
    assert response.json()["isHealthy"] is False


def test_build_app_instruments_when_app_insights_connection_string_set(monkeypatch):
    """Regression: enabling Application Insights must not break app startup or routing."""
    monkeypatch.setenv(
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://fake.example.com/",
    )
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    assert client.get("/Wake").status_code == 200
    assert client.get("/About").status_code == 200


def test_build_app_ignores_whitespace_only_app_insights_connection_string(monkeypatch):
    """A whitespace-only connection string is treated as unset (mirrors .NET's IsNullOrWhiteSpace)."""
    monkeypatch.setenv("APPLICATIONINSIGHTS_CONNECTION_STRING", "   ")
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    assert client.get("/Wake").status_code == 200


def test_about_reports_build_metadata(monkeypatch):
    monkeypatch.setenv("BUILD_NUMBER", "42")
    monkeypatch.setenv("BUILD_START", "2026-01-02T03:04:05Z")
    monkeypatch.setenv("BUILD_BRANCH_NAME", "main")
    app = _build_test_app(monkeypatch)
    client = TestClient(app)
    body = client.get("/About").json()
    assert body["buildNumber"] == 42
    assert body["buildStart"] == "2026-01-02T03:04:05Z"
    assert body["buildBranchName"] == "main"
