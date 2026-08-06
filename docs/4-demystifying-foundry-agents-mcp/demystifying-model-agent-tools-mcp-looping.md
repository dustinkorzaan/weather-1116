# Looping diagrams — tool callbacks and MCP

Companion to [`demystifying-foundry-agents-mcp.md`](demystifying-foundry-agents-mcp.md) and
[`demystifying-model-agent-tools-mcp-brainstorming.md`](demystifying-model-agent-tools-mcp-brainstorming.md).

End-to-end sequences where the **agent/loop** owns the turn cycle: the model requests
tools, the app or MCP host executes them, results go back to the model until a final
answer is produced.

## Tool callback diagram (with looping)

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
    GetLatLongFunc-->>AppLoop: NonAILatLongResponse
    AppLoop-->>Model: NonAILatLongResponse
    Model->>AppLoop: GetPublicWeather(lat,long)
    AppLoop->>GetPublicWeatherFunc: GetPublicWeather(lat,long)
    GetPublicWeatherFunc-->>AppLoop: NonAIWeatherResponse
    AppLoop-->>Model: NonAIWeatherResponse
    Model-->>AppLoop: AIWeatherResponse
    AppLoop-->>API: AIWeatherResponse
    API-->>UI: AIWeatherResponse
```

## Agent to MCP (with looping)

```mermaid
sequenceDiagram
    autonumber
    participant Console
    participant AppLoop as Agent/Loop
    participant Model as Foundry Model
    box MCP Function
        participant GetLatLongTool
    end
    box MCP DotNet
        participant GetPublicWeatherTool
    end

    Console->>AppLoop: system prompt + MCP tools, user prompt last
    AppLoop->>Model: system prompt + MCP tools, user prompt last
    Model->>AppLoop: GetLatLong(location)
    AppLoop->>GetLatLongTool: GetLatLong(location)
    GetLatLongTool-->>AppLoop: NonAILatLongResponse
    AppLoop-->>Model: NonAILatLongResponse
    Model->>AppLoop: GetPublicWeather(lat,long)
    AppLoop->>GetPublicWeatherTool: GetPublicWeather(lat,long)
    GetPublicWeatherTool-->>AppLoop: NonAIWeatherResponse
    AppLoop-->>Model: NonAIWeatherResponse
    Model-->>AppLoop: AIWeatherResponse
    AppLoop-->>Console: AIWeatherResponse
```
