# MCP Inspection

How to examine this repo's two remote MCP hosts (`mcp-srv-app-service`,
`mcp-srv-func-app`) directly - outside a chat tab or Foundry console - with
the MCP Inspector, Postman, and curl.

## MCP 2026-07-28 vs Legacy

* [MCP spec changelog - 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28/changelog)
* [MCP 2026-07-28: the big architectural shift](https://securityboulevard.com/2026/08/mcp-2026-07-28-the-big-architectural-shift)

`mcp-srv-app-service` already runs in **stateless** mode
(`WithHttpTransport(options => options.Stateless = true)` in
[`Program.cs`](../../mcp-srv-app-service/mcp/Program.cs)) - no session
required across requests, no server-initiated SSE stream. That is why a
`GET /mcp` returns `405` in the Postman console below: stateless HTTP
transport only accepts `POST`. Keep that in mind when comparing behavior
against the legacy (stateful, session-and-SSE) transport described in the
links above.

## MCP Inspector

* [`modelcontextprotocol.io/docs/2026-07-28/tools/inspector`](https://modelcontextprotocol.io/docs/2026-07-28/tools/inspector)

* bash `npx @modelcontextprotocol/inspector`

Point it at:

* Local `mcp-srv-app-service` - `http://localhost:8110/mcp`, header
  `Authorization: Bearer <MCP_SRV_APP_SERVICE_KEY>`
* Local `mcp-srv-func-app` - `http://localhost:8120/runtime/webhooks/mcp`,
  header `x-functions-key: <mcp_extension system key>`
* Prod hosts - see [`docs/architecture.md`](../architecture.md#mcp-tool-hosts)
  for the production URLs and auth headers

## Postman

Add a request to a Postman collection pointed at the `/mcp` endpoint (
`https://weather1116-prod-mcp-srv-app-service-<slot>.westus2-01.azurewebsites.net/mcp`):

* Method **POST**, header `Authorization: Bearer <MCP_SRV_APP_SERVICE_KEY>`
* Body → raw JSON, a JSON-RPC request, e.g. `tools/call` for
  `GetPublicWeatherCurrent` with `latitude`/`longitude` arguments
* Postman renders the tools list under the **Tools** tab (once a collection
  is generated from the MCP endpoint) and the JSON-RPC result under
  **Response**

![Postman against the prod mcp-srv-app-service /mcp endpoint, showing the GetPublicWeatherCurrent tool and its response](postman-mcp-tools.png)

The **Console** tab in the screenshot above shows the request sequence
against the stateless endpoint: an initial `POST` failing (`400`) before the
session handshake, the `initialize` `POST` succeeding (`200`), a
notification accepted (`202`), a `GET` rejected (`405` - no SSE stream in
stateless mode), and the `tools/call` `POST`s for
`GetPublicWeatherCurrent`/`GetPublicWeatherHistory` returning `200` with the
tool's JSON content.

## curl example

```bash
curl -sS -N -X POST "https://weather1116-prod-mcp-srv-app-service-gdaef6e5cndqb3du.westus2-01.azurewebsites.net/mcp" \
  -H "accept: application/json, text/event-stream" \
  -H "authorization: Bearer ..." \
  -H "content-type: application/json" \
  -H "mcp-protocol-version: 2025-11-25" \
  --data '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-11-25",
      "capabilities": {},
      "clientInfo": {
        "name": "curl",
        "version": "1.0"
      }
    }
  }' \
  | sed -n 's/^[[:space:]]*data:[[:space:]]*//p' \
  | python -m json.tool
```

```bash
curl -sS -X POST "https://weather1116-prod-mcp-srv-app-service-gdaef6e5cndqb3du.westus2-01.azurewebsites.net/mcp" \
  -H "accept: application/json, text/event-stream" \
  -H "authorization: Bearer ..." \
  -H "content-type: application/json" \
  -H "mcp-protocol-version: 2025-11-25" \
  --data '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' \
  | sed -n 's/^[[:space:]]*data:[[:space:]]*//p' \
  | python -m json.tool
```

## Related docs

* [`docs/architecture.md`](../architecture.md) - MCP Tool Hosts section: ports, endpoints, auth headers, prod URLs
* [`docs/5-chat-clients/5-chat-clients.md`](../5-chat-clients/5-chat-clients.md) - Chat1b/Chat2b/Chat3 remote-MCP usage
* [`docs/presentation.md`](../presentation.md) - talk index
