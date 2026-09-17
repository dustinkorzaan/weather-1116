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


def test_mcp_rejects_all_requests_when_key_unset(monkeypatch):
    monkeypatch.delenv("MCP_SRV_PYTHON_KEY", raising=False)
    from weather_mcp_srv_python.server import build_app

    app = build_app()
    client = TestClient(app)
    response = client.post("/mcp")
    assert response.status_code == 401
