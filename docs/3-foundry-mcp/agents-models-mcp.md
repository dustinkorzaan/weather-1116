# Agents, Models, and MCP

See also [`docs/architecture.md`](../architecture.md) for runtime diagrams and production
settings.

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
  - **Examples 1–2:** model-only — no Core weather data (ex 1 fails; ex 2 invents an answer).
  - **Examples 3–4:** console pre-fetches lat/long and weather from Core, injects JSON
    into the prompt, then calls the model (string out in ex 3; `AIWeatherResponse` JSON in ex 4).

  Examples 1–2 (model only):

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Model as Foundry Model

      Console->>Model: weather question (location)
      Model-->>Console: unreliable text
  ```

  Examples 3–4 (console-driven Core prep, then model):

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant GetLatLong
      participant GetPublicWeather
      participant Model as Foundry Model

      Console->>GetLatLong: GetLatLong(location)
      GetLatLong-->>Console: NonAILatLongResponse
      Console->>GetPublicWeather: GetPublicWeather(lat,long)
      GetPublicWeather-->>Console: NonAIWeatherResponse
      Console->>Model: prompt + weather JSON
      Model-->>Console: text (ex 3) or AIWeatherResponse (ex 4)
  ```

- **V2 — Model-direct (unified AI endpoint)** — [`FoundryConsoleV2`](../../FoundryConsoleV2)
  (`FoundryConsoleV2ModelDirectUnifiedAI.csproj`)
  - Same idea as V1 but uses `ResponsesClient` against the unified AI services
    endpoint.
  - Shows the newer Foundry / Azure AI inference surface.

- **V3 — In-process function tools** — [`FoundryConsoleV3`](../../FoundryConsoleV3)
  (`FoundryConsoleV3InjectFunctions.csproj`)
  - Registers `GetLatLongData` and `GetPublicWeatherData` as injected function
    tools handled in-process (same tools `Core` exposes).
  - Model chooses tools locally; no remote MCP servers yet.

  ```mermaid
  sequenceDiagram
      autonumber
      participant UI
      box API
          participant API
          participant GetPublicWeatherFunc
          participant GetLatLongFunc
      end
      participant Model as Foundry Model

      UI->>API: GetPublicWeather(location)
      API->>Model: GetPublicWeather(location)
      Model->>GetLatLongFunc: GetLatLong(location)
      GetLatLongFunc-->>Model: NonAILatLongResponse
      Model->>GetPublicWeatherFunc: GetPublicWeather(lat,long)
      GetPublicWeatherFunc-->>Model: NonAIWeatherResponse
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
      participant API
      participant Agent as Foundry Agent
      box MCP Function
          participant GetLatLongTool
      end
      box MCP DotNet
          participant GetPublicWeatherTool
      end

      UI->>API: GetPublicWeather(location)
      API->>Agent: GetPublicWeather(location)
      Agent->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Agent: NonAILatLongResponse
      Agent->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Agent: NonAIWeatherResponse
      Agent-->>API: AIWeatherResponse
      API-->>UI: AIWeatherResponse
  ```
