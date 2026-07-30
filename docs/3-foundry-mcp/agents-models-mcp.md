# Agents, Models, and MCP

How this sample wires **Microsoft Foundry** agents, direct model calls, and
**Model Context Protocol (MCP)** tool hosts together. See also
[`docs/architecture.md`](../architecture.md) for runtime diagrams and production
settings.

## Production path (API / MVC)

Current AI Weather in React, Blazor, and MVC ultimately calls a **hosted Foundry
agent** (default `wx1116-agent-default`) via
`Core.AIWeather.Handlers.GetCurrentAIWeatherHandler`. The agent resolves a
place name and fetches weather using MCP tools that map back to this repo's
`Core` handlers.

| Host | Path | Tool | Port | Endpoint |
| --- | --- | --- | --- | --- |
| MCP DotNet | [`mcp-dotnet/mcp`](../../mcp-dotnet/mcp) | `GetPublicWeatherData` | 8110 | `/mcp` |
| MCP Function | [`mcp-function/mcp`](../../mcp-function/mcp) | `GetLatLongData` | 8120 | `/runtime/webhooks/mcp` |

Required settings: `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL`, `AZURE_FOUNDRY_PROD_EUS2_KEY`,
and optionally `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME`. MCP auth and prod URLs are
documented in [`architecture.md`](../architecture.md#mcp-tool-hosts).

## Foundry console demos (learning path)

Four standalone console apps in `Weather.sln` show how the production agent
pattern was built up. They are **training building blocks**, not deployables.
Run from VS Code (**Foundry Console V1** … **V4**) or `dotnet run` in each
folder. All use the `AZURE_FOUNDRY_PROD_EUS2_*` prefix (see each `.env.example`).

Suggested order: **V1 → V2 → V3 → V4 →** `GetCurrentAIWeatherHandler` in
`core-dotnet/core/AIWeather`.

- **V1 — Model-direct (legacy endpoint)** — [`FoundryConsoleV1`](../../FoundryConsoleV1)
  (`FoundryConsoleV1ModelDirectLegacy.csproj`)
  - Calls the model directly via legacy `AzureOpenAIClient` / Cognitive Services
    endpoint.
  - No agent; no MCP; demonstrates baseline chat completion against Foundry/OpenAI.

- **V2 — Model-direct (unified AI endpoint)** — [`FoundryConsoleV2`](../../FoundryConsoleV2)
  (`FoundryConsoleV2ModelDirectUnifiedAI.csproj`)
  - Same idea as V1 but uses `ResponsesClient` against the unified AI services
    endpoint.
  - Shows the newer Foundry / Azure AI inference surface.

- **V3 — In-process function tools** — [`FoundryConsoleV3`](../../FoundryConsoleV3)
  (`FoundryConsoleV3InjectFunctions.csproj`)
  - Registers `GetLatLongData` and `GetPublicWeatherData` as injected function
    tools handled in-process via MediatR (same tools `Core` exposes).
  - Model chooses tools locally; no remote MCP servers yet.

  ```mermaid
  sequenceDiagram
      autonumber
      participant UI
      box WeatherAPI
          participant API as WeatherAPI
          participant GetPublicWeatherTool
          participant GetLatLongTool
      end
      participant Model as Foundry Model

      UI->>API: GetPublicWeather(location)
      API->>Model: GetPublicWeather(location)
      Model->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Model: NonAILatLongResponse
      Model->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Model: NonAIWeatherResponse
      Model-->>API: AIWeatherResponse
      API-->>UI: AIWeatherResponse
  ```

- **V4 — Hosted Foundry agent + MCP** — [`FoundryConsoleV4`](../../FoundryConsoleV4)
  (`FoundryConsoleV4MCP.csproj`)
  - Calls the **same hosted Foundry agent** API and MVC use (`wx1116-agent-default`
    by default).
  - Agent invokes MCP lat/long and weather tools (`mcp-function`, `mcp-dotnet`).
  - **V4-only settings** (same `AZURE_FOUNDRY_PROD_EUS2_*` prefix as V1–V3):
    - `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL` (required) — Foundry project URL.
    - `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME` (optional) — defaults to
      `wx1116-agent-default`.
    - `AZURE_FOUNDRY_PROD_EUS2_KEY` (required) — same API key as V1–V3.

  ```mermaid
  sequenceDiagram
      autonumber
      participant UI
      box WeatherAPI
          participant API as WeatherAPI
      end
      participant Model as Foundry Model
      participant GetLatLongTool
      participant GetPublicWeatherTool

      UI->>API: GetPublicWeather(location)
      API->>Model: GetPublicWeather(location)
      Model->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Model: NonAILatLongResponse
      Model->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Model: NonAIWeatherResponse
      Model-->>API: AIWeatherResponse
      API-->>UI: AIWeatherResponse
  ```
