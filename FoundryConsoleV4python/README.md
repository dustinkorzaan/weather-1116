# Foundry Console V4 (Python)

Python port of [`FoundryConsoleV4`](../FoundryConsoleV4). It uses the Responses API with **remote MCP servers** as tools. The service calls the four deployed MCP hosts itself, so there's no local tool-call loop and a single `responses.create` call returns strict JSON.

| Server label | Host | Auth header |
| --- | --- | --- |
| `McpSrvFuncApp` | `mcp-srv-func-app` | `x-functions-key` |
| `McpSrvAppService` | `mcp-srv-app-service` | `Authorization: Bearer` |
| `McpSrvPython` | `mcp-srv-python` | `Authorization: Bearer` |
| `McpSrvNode` | `mcp-srv-node` | `Authorization: Bearer` |

The server URLs are hardcoded to the prod ACA FQDNs, the same ones the C# console uses.

## Run

```bash
cd FoundryConsoleV4python
pip install -e ".[dev]"
cp .env.example .env   # set AZURE_FOUNDRY_PROD_KEY and the four MCP_SRV_*_KEY values
foundry-console-v4-python   # or: python foundry_console_v4.py
```

## Test

```bash
python -m pytest -q
```
