import pytest

from weather_mcp_srv_python.tools import geo


@pytest.fixture(autouse=True)
def _clear_geo_cache():
    geo.clear_cache()
    yield
    geo.clear_cache()
