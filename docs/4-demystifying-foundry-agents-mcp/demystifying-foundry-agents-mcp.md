# Demystifying Microsoft Foundry Agents and MCP

See also [`docs/architecture.md`](../architecture.md) for runtime diagrams and production
settings.

## Foundry console demos (learning path)

Five standalone console apps in `Weather.sln` show how the production AI weather
path was built up, with **V5** as a hosted-agent contrast. They are **training
building blocks**, not deployables.
Run from VS Code (**Foundry Console V1** … **V5**) or `dotnet run` in each
folder. All use the `AZURE_FOUNDRY_PROD_EUS2_*` prefix (see each `.env.example`).

Suggested order: **V1 → V2 → V3 → V4 →** `GetCurrentAIWeatherHandler` in
`core-dotnet/core/AIWeather` **→ V5**.

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

- **V3 — In-process tool callbacks** — [`FoundryConsoleV3`](../../FoundryConsoleV3)
  (`FoundryConsoleV3InProcessToolCallbacks.csproj`)
  - Registers `GetLatLongData` and `GetPublicWeatherData` as in-process tool
    callbacks (same tools `Core` exposes).
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

- **V4 — Model-direct + remote MCP tools** — [`FoundryConsoleV4`](../../FoundryConsoleV4)
  (`FoundryConsoleV4MCP.csproj`)
  - Same model-direct call as V3, but the tools are **remote MCP servers**
    declared on the request instead of in-process callbacks — no agent, and no
    Foundry-specific client.
  - The service calls the MCP servers itself, so V3's tool-call loop disappears.
    ("kind of", more details later)
  - Shows that MCP tooling does not require a Foundry agent.
  - Same pattern as production `GetCurrentAIWeatherHandler` in API/MVC.

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Model as Foundry Model
      box MCP Function
          participant GetLatLongTool
      end
      box MCP DotNet
          participant GetPublicWeatherTool
      end

      Console->>Model: system prompt + MCP tools, user prompt last
      Model->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Model: NonAILatLongResponse
      Model->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Model: NonAIWeatherResponse
      Model-->>Console: AIWeatherResponse
  ```

- **V5 — Hosted Foundry agent + MCP** — [`FoundryConsoleV5`](../../FoundryConsoleV5)
  (`FoundryConsoleV5Agent.csproj`)
  - Agent-hosted alternative to V4: calls a **hosted Foundry agent**
    (`wx1116-agent-default` by default).
  - Instructions, response schema, and MCP tools (`mcp-function`, `mcp-dotnet`)
    are configured on the agent in Azure.
  - The console sends **only the user prompt** — Responses `instructions` and
    `text` fields are rejected when an agent is specified.
  - **V5-only settings** (same `AZURE_FOUNDRY_PROD_EUS2_*` prefix as V1–V4):
    - `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL` (required) — Foundry project URL.
    - `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME` (optional) — defaults to
      `wx1116-agent-default`.
    - `AZURE_FOUNDRY_PROD_EUS2_KEY` (required) — same API key as V1–V3.

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Agent as Foundry Agent
      box MCP Function
          participant GetLatLongTool
      end
      box MCP DotNet
          participant GetPublicWeatherTool
      end

      Console->>Agent: user prompt only
      Agent->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Agent: NonAILatLongResponse
      Agent->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Agent: NonAIWeatherResponse
      Agent-->>Console: AIWeatherResponse
  ```

## Microsoft reference

[Azure AI Foundry Agents overview](https://learn.microsoft.com/en-us/azure/foundry/agents/overview)

![What is an agent? — Azure AI Foundry](https://learn.microsoft.com/en-us/azure/foundry/agents/media/what-is-an-agent.png)

See also [brainstorming](demystifying-model-agent-tools-mcp-brainstorming.md) and
[tool callback looping](demystifying-model-agent-tools-mcp-looping.md).
