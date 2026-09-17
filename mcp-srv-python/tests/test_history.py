import pytest

from weather_mcp_srv_python.tools.history import _build_history_url, _normalize_series_block, _DAILY_KEYS


def test_build_history_url_daily():
    url = _build_history_url(47.6062, -122.3321, "daily")
    assert url.startswith("https://api.open-meteo.com/v1/forecast?")
    assert "past_days=7" in url
    assert "forecast_days=0" in url


def test_build_history_url_hourly():
    url = _build_history_url(0, 0, "hourly")
    assert "past_hours=48" in url
    assert "forecast_hours=0" in url


def test_build_history_url_rejects_unknown_resolution():
    with pytest.raises(ValueError):
        _build_history_url(0, 0, "bogus")


def test_normalize_series_block_clamps_daily_precipitation_sum():
    block = {
        "time": ["2024-01-01"],
        "weather_code": [1],
        "temperature_2m_max": [10.0],
        "temperature_2m_min": [5.0],
        "precipitation_sum": [-1.0, 2.0],
        "wind_speed_10m_max": None,
        "wind_direction_10m_dominant": None,
    }
    _normalize_series_block(block, _DAILY_KEYS)
    assert block["precipitation_sum"] == [0, 2.0]
    assert block["wind_speed_10m_max"] == []
    assert block["wind_direction_10m_dominant"] == []
