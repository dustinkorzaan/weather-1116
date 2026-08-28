# Presentation: The Journey

<p align="center">
  <a href="https://github.com/dustinkorzaan/weather-1116">
    <img src="qr-code-white-circle-background.svg" alt="QR code — GitHub repo" width="200" />
  </a>
</p>

Sections **1–3** are framing — name them, show the folder, move on. The material is
sketched, not presentation-ready. **Section 4** is the focus: live demos, architecture,
and the Foundry console learning path (V1 → V5).

## Sections

| | Title | Doc | Role in the talk |
| --- | --- | --- | --- |
| 1 | Laptop-free Engineering | [`laptop-free.md`](1-laptop-free-engineering/laptop-free.md) | Introduce — context for cloud / Codespaces / VPC |
| 2 | Autonomous Sprint Board AI Development | [`autonomous-sprint-board.md`](2-autonomous-sprint-board-ai-development/autonomous-sprint-board.md) | Introduce — placeholder for sprint-board + agents story |
| 3 | AI Development Ecosystem | [`ai-development-ecosystem.md`](3-ai-development-ecosystem/ai-development-ecosystem.md) | Introduce — landscape of tools (wireframes, rapid app gen, IDEs) |
| 4 | Demystifying Foundry, agents, and models | [`demystifying-foundry-agents-mcp.md`](4-demystifying-foundry-agents-mcp/demystifying-foundry-agents-mcp.md), [`brainstorming`](4-demystifying-foundry-agents-mcp/demystifying-model-agent-tools-mcp-brainstorming.md) | **Focus** — Foundry consoles, agents, MCP hosts in this repo |
| 5 | Chat Clients (Chat1a–Chat3) | [`5-chat-clients.md`](5-chat-clients/5-chat-clients.md) | **Hands-on** — five chat tabs in React/MVC/Blazor |
| 6 | MCP Inspection | [`6-mcp-inspection.md`](6-mcp-inspection/6-mcp-inspection.md) | **Hands-on** - Inspector, Playground, Postman, and curl against the MCP hosts |

## Supporting material

- [`architecture.md`](architecture.md) — runtime diagrams, ports, production settings (pair with section 4)
- [`../README.md`](../README.md) — how to run the Weather sample locally or in Codespaces
- [`../AGENTS.md`](../AGENTS.md) — Cursor Cloud / agent environment notes

## Suggested flow (section 4)

1. **Problem**: AI weather needs real lat/long and public weather data, not hallucination.
   - Location `"Nashville, TN"` → Lat/Long `"36.166° N, 86.784° W"`
     - `GetLatLongEvent(location)` returns ranked lat/long matches (default 5; V1/V2 use top 1)
   - Lat/Long → Non-AI Weather `{ temp: 24, ... }`
     - `GetPublicWeatherCurrentEvent(Lat/Long)` returns Non-AI Weather
   - Non-AI Weather → AI Summary `"Currently it is 75 °F in Nashville, TN ..."`
     - Model derives and returns summary
2. **V1 → V2**: model-direct (legacy vs unified endpoint); when the console still owns data prep.
3. **V3**: local in-process tool loops; model chooses tools, your console answers them.
4. **V4**: model-direct with remote MCP tools
5. **V5**: hosted Foundry agent; agent owns the instructions, response schema, and MCP tools
6. **Live Demos**
   - Live links: [React UI](https://wx.korzaan.com), [Blazor UI](https://weather1116-prod-blazor.azurewebsites.net), [MVC UI](https://weather1116-prod-mvc.azurewebsites.net)
   - Current AI Weather links: [React UI](https://wx.korzaan.com/current-ai-weather), [Blazor UI](https://weather1116-prod-blazor.azurewebsites.net/current-ai-weather), [MVC UI](https://weather1116-prod-mvc.azurewebsites.net/current-ai-weather)
   - Chat links: [React UI](https://wx.korzaan.com/chat-clients), [Blazor UI](https://weather1116-prod-blazor.azurewebsites.net/chat-clients), [MVC UI](https://weather1116-prod-mvc.azurewebsites.net/chat-clients)

Adjust the first 2 to taste, keep the main palette focused on the next 3, and finish with the last live UI demos for dessert.
