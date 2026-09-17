import pytest

from weather_mcp_srv_python.tools.forecast import _build_forecast_url, _normalize_series_block, _SUB_HOURLY_KEYS


def test_build_forecast_url_daily():
    url = _build_forecast_url(47.6062, -122.3321, "daily")
    assert url.startswith("https://api.open-meteo.com/v1/forecast?")
    assert "latitude=47.6062" in url
    assert "longitude=-122.3321" in url
    assert "forecast_days=7" in url
    assert "temperature_2m_max" in url
    assert "temperature_unit=celsius" in url
    assert "wind_speed_unit=kmh" in url
    assert "precipitation_unit=mm" in url
    assert "timezone=auto" in url


def test_build_forecast_url_hourly():
    url = _build_forecast_url(0, 0, "hourly")
    assert "forecast_hours=48" in url
    assert "hourly=" in url


def test_build_forecast_url_fifteen_minutes():
    url = _build_forecast_url(0, 0, "fifteen_minutes")
    assert "forecast_minutely_15=192" in url
    assert "minutely_15=" in url


def test_build_forecast_url_rejects_unknown_resolution():
    with pytest.raises(ValueError):
        _build_forecast_url(0, 0, "bogus")


def test_normalize_series_block_fills_nulls_and_clamps_precipitation():
    block = {
        "time": None,
        "temperature_2m": [1.0],
        "precipitation": [-2.0, 3.0],
        "weather_code": None,
        "wind_speed_10m": None,
        "wind_direction_10m": None,
    }
    _normalize_series_block(block, _SUB_HOURLY_KEYS)
    assert block["time"] == []
    assert block["weather_code"] == []
    assert block["precipitation"] == [0, 3.0]


def test_normalize_series_block_handles_none_block():
    _normalize_series_block(None, _SUB_HOURLY_KEYS)
