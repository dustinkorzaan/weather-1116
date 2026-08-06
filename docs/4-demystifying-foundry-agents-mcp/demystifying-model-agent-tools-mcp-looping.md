# Tool callback diagram (with looping)

Companion to [`demystifying-foundry-agents-mcp.md`](demystifying-foundry-agents-mcp.md) and
[`demystifying-model-agent-tools-mcp-brainstorming.md`](demystifying-model-agent-tools-mcp-brainstorming.md).

End-to-end sequence for **in-process tool callbacks**: the agent/loop owns the turn
cycle — the model requests tools, the app executes them, results go back to the model
until a final answer is produced.

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
