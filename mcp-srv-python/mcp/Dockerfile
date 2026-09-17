FROM python:3.12-slim AS base
WORKDIR /app

COPY mcp-srv-python/mcp/pyproject.toml mcp-srv-python/mcp/
COPY mcp-srv-python/mcp/weather_mcp_srv_python mcp-srv-python/mcp/weather_mcp_srv_python

RUN pip install --no-cache-dir ./mcp-srv-python/mcp

ENV MCP_SRV_PYTHON_HOST=0.0.0.0
ENV MCP_SRV_PYTHON_PORT=8080
EXPOSE 8080

ENTRYPOINT ["weather-mcp-srv-python"]
