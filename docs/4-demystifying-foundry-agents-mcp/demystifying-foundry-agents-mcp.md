# Demystifying Foundry, agents, and models

Demystifying Foundry, agents, and models: from model-direct, to local
in-process looping, to remote MCP, to a hosted agent, behind a pin map using
the following core weather concepts:

- Location `"Nashville, TN"` → Lat/Long `"36.166° N, 86.784° W"`
- Lat/Long → Non-AI Weather `{ temp: 75, conditions: "partly cloudy", ... }`
- Non-AI Weather → AI Summary `"Currently it is 75 °F in Nashville, TN ..."`

## Microsoft reference

[Microsoft Foundry Agents overview](https://learn.microsoft.com/en-us/azure/foundry/agents/overview)

![What is an agent? — Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/agents/media/what-is-an-agent.png)

See also [`docs/architecture.md`](../architecture.md) and
[`docs/.../...brainstorming.md`](demystifying-model-agent-tools-mcp-brainstorming.md).

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
      GetLatLong-->>Console: NonAILatLongListResponse (V1/V2 use top 1)
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

- **V3 — Local in-process looping** — [`FoundryConsoleV3`](../../FoundryConsoleV3)
  (`FoundryConsoleV3InProcessToolCallbacks.csproj`)
  - Registers `GetLatLong`, `GetLocation`, `GetPublicWeatherCurrent`, `GetPublicWeatherForecast`, and `GetPublicWeatherHistory` as tools
    answered by local in-process looping (same Core code reused in tool).
  - Model chooses tools that are actually handled locally; no remote MCP servers yet.

  **Simple Diagram without Agent/Loop**

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
      GetLatLongFunc-->>Model: NonAILatLongListResponse
      Model->>GetPublicWeatherFunc: GetPublicWeather(lat,long)
      GetPublicWeatherFunc-->>Model: NonAIWeatherResponse
      Model-->>API: AIWeatherResponse
      API-->>UI: AIWeatherResponse
  ```

  **Diagram with Agent/Looping**

  ```mermaid
  sequenceDiagram
      autonumber
      participant UI
      box API
          participant API
          participant AppLoop as Agent/Loop
          participant GetPublicWeatherFunc
          participant GetLatLongFunc
      end
      participant Model as Foundry Model

      UI->>API: GetPublicWeather(location)
      API->>AppLoop: GetPublicWeather(location)
      AppLoop->>Model: GetPublicWeather(location)
      Model->>AppLoop: GetLatLong(location)
      AppLoop->>GetLatLongFunc: GetLatLong(location)
      GetLatLongFunc-->>AppLoop: NonAILatLongListResponse
      AppLoop-->>Model: NonAILatLongListResponse
      Model->>AppLoop: GetPublicWeather(lat,long)
      AppLoop->>GetPublicWeatherFunc: GetPublicWeather(lat,long)
      GetPublicWeatherFunc-->>AppLoop: NonAIWeatherResponse
      AppLoop-->>Model: NonAIWeatherResponse
      Model-->>AppLoop: AIWeatherResponse
      AppLoop-->>API: AIWeatherResponse
      API-->>UI: AIWeatherResponse
  ```

- **V4 — Model-direct + remote MCP tools** — [`FoundryConsoleV4`](../../FoundryConsoleV4)
  (`FoundryConsoleV4MCP.csproj`)
  - Same model-direct call as V3, but the tools are hosted in remote MCP servers
    declared on the request instead of in-process callbacks.
  - Shows that MCP tooling does not require a Foundry agent.
  - Same pattern as production `GetCurrentAIWeatherHandler` in API/MVC.

  **Simple Diagram without Agent/Loop**

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Model as Foundry Model
      box MCP Server on Functions App
          participant GetLatLongTool
      end
      box MCP Server on App Service
          participant GetPublicWeatherTool
      end

      Console->>Model: system prompt + MCP tools, user prompt last
      Model->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Model: NonAILatLongListResponse
      Model->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Model: NonAIWeatherResponse
      Model-->>Console: AIWeatherResponse
  ```

  **Diagram with Agent/Looping**

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant AppLoop as Agent/Loop
      participant Model as Foundry Model
      box MCP Server on Functions App
          participant GetLatLongTool
      end
      box MCP Server on App Service
          participant GetPublicWeatherTool
      end

      Console->>AppLoop: system prompt + MCP tools, user prompt last
      AppLoop->>Model: system prompt + MCP tools, user prompt last
      Model->>AppLoop: GetLatLong(location)
      AppLoop->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>AppLoop: NonAILatLongListResponse
      AppLoop-->>Model: NonAILatLongListResponse
      Model->>AppLoop: GetPublicWeather(lat,long)
      AppLoop->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>AppLoop: NonAIWeatherResponse
      AppLoop-->>Model: NonAIWeatherResponse
      Model-->>AppLoop: AIWeatherResponse
      AppLoop-->>Console: AIWeatherResponse
  ```

- **V5 — Hosted Foundry agent + MCP** — [`FoundryConsoleV5`](../../FoundryConsoleV5)
  (`FoundryConsoleV5Agent.csproj`)
  - Agent-hosted alternative to V4: calls a **hosted Foundry agent**
    (`wx1116-agent-default` by default).
  - Instructions, response schema, and MCP tools (`mcp-srv-func-app`, `mcp-srv-app-service`)
    are configured on the agent in Azure.
  - The console sends **only the user prompt** — Responses `instructions` and
    `text` fields are rejected when an agent is specified.

  **Simple Diagram without Agent/Loop**

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Agent as Foundry Agent
      box MCP Server on Functions App
          participant GetLatLongTool
      end
      box MCP Server on App Service
          participant GetPublicWeatherTool
      end

      Console->>Agent: user prompt only
      Agent->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Agent: NonAILatLongListResponse
      Agent->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Agent: NonAIWeatherResponse
      Agent-->>Console: AIWeatherResponse
  ```

  **Diagram with Agent/Looping**

  ```mermaid
  sequenceDiagram
      autonumber
      participant Console
      participant Agent as Foundry Agent
      participant Model as Foundry Model
      box MCP Server on Functions App
          participant GetLatLongTool
      end
      box MCP Server on App Service
          participant GetPublicWeatherTool
      end

      Console->>Agent: user prompt only
      Agent->>Model: user prompt only
      Model->>Agent: GetLatLong(location)
      Agent->>GetLatLongTool: GetLatLong(location)
      GetLatLongTool-->>Agent: NonAILatLongListResponse
      Agent-->>Model: NonAILatLongListResponse
      Model->>Agent: GetPublicWeather(lat,long)
      Agent->>GetPublicWeatherTool: GetPublicWeather(lat,long)
      GetPublicWeatherTool-->>Agent: NonAIWeatherResponse
      Agent-->>Model: NonAIWeatherResponse
      Model-->>Agent: AIWeatherResponse
      Agent-->>Console: AIWeatherResponse
  ```
