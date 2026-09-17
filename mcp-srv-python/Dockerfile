FROM python:3.12-slim AS base
WORKDIR /app

COPY mcp-srv-python/pyproject.toml mcp-srv-python/
COPY mcp-srv-python/src mcp-srv-python/src

RUN pip install --no-cache-dir ./mcp-srv-python

ENV MCP_SRV_PYTHON_HOST=0.0.0.0
ENV MCP_SRV_PYTHON_PORT=8080
EXPOSE 8080

ENTRYPOINT ["weather-mcp-srv-python"]
