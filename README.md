# Weather 1116

Demystify Microsoft Foundry, AI agents, and AI Models. This repository
provides an evolution across architectural patterns: model-direct
execution, local in-process tool loops, remote Model Context Protocol
(MCP) integrations, and fully hosted remote agents.

Start with local console prototypes, end with a live UI demo.

Presentation Reference: [`docs/presentation.md`](docs/presentation.md)

## Console Demos

This repository features local console applications built with Microsoft
Foundry and Azure OpenAI. It contrasts local implementations (V1–V4)
against a fully remote, hosted Foundry Agent (V5) through a 3-step core
weather progression:

- Location `"Nashville, TN"` → Lat/Long `"36.166° N, 86.784° W"`
- Lat/Long → Non-AI Weather `{ temp: 24, ... }`
- Non-AI Weather → AI Summary `"Currently it is 75 °F in Nashville, TN ..."`

|  | Project | Description |
| --- | --- | --- |
| V1 | [`Foundry Console V1 Model Direct Legacy`](FoundryConsoleV1) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`Foundry Console V2 Model Direct Unified AI`](FoundryConsoleV2) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`Foundry Console V3 In Process Tool Callbacks`](FoundryConsoleV3) | Model-direct: tools handled by local in-process tool loops |
| V4 | [`Foundry Console V4 MCP`](FoundryConsoleV4) | Model-direct: tools handled by remote MCP servers |
| V5 | [`Foundry Console V5 Agent`](FoundryConsoleV5) | Hosted Foundry Agent owns the instructions, response schema, and MCP tools; console sends only the user prompt |

[Visit](https://wx.korzaan.com/current-ai-weather) the React UI, wired to V3, V4, and V5.

## Chat clients

Presentation reference: [`docs/5-chat-clients/5-chat-clients.md`](docs/5-chat-clients/5-chat-clients.md)

Standalone multi-turn chat page with five tabs:

| Tab | Pattern | Stack | Notes |
| --- | --- | --- | --- |
| Chat1a | In-process | Responses API | Like Foundry Console V3 |
| Chat1b | Remote MCP | Responses API | Like Foundry Console V4 |
| Chat2a | In-process | Agent Framework | Like Foundry Console V3 |
| Chat2b | Remote MCP | Agent Framework | Like Foundry Console V4 |
| Chat3 | Hosted Foundry agent | Fully managed agent orchestration | Like Foundry Console V5 (`wx1116-agent-chat`) |

## MCP inspection

Presentation reference: [`docs/6-mcp-inspection/6-mcp-inspection.md`](docs/6-mcp-inspection/6-mcp-inspection.md)

How to poke at `mcp-srv-app-service` and `mcp-srv-func-app` directly with the
MCP Inspector, MCP Playground, Postman, or curl — outside a chat tab or
Foundry console.

## Projects, Architecture, and live demos in a Weather Pin App

|  | Project | Path | Stack | Port |
| --- | --- | --- | --- | --- |
| [Visit](https://wx.korzaan.com) | React UI | [`ui-react`](ui-react) | React + Vite | 3000 |
| [Visit](https://weather1116-prod-blazor.azurewebsites.net) | Blazor UI | [`ui-blazor/blazor`](ui-blazor/blazor) | Blazor | 8090 |
| [Visit](https://weather1116-prod-mvc.azurewebsites.net) | MVC UI | [`mvc-dotnet/mvc`](mvc-dotnet/mvc) | ASP.NET Core MVC | 8100 |
|  | API | [`api-dotnet/api`](api-dotnet/api) | ASP.NET Core Minimal API | 8080 |
|  | Core | [`core-dotnet/core`](core-dotnet/core) | In API, MVC, Worker, and MCP |  |
|  | Worker DotNet | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire dashboard and servers | 8130 |
|  | MCP Server (App Service) | [`mcp-srv-app-service/mcp`](mcp-srv-app-service/mcp) | ASP.NET Core MCP server | 8110 |
|  | MCP Server (Function App) | [`mcp-srv-func-app/mcp`](mcp-srv-func-app/mcp) | Azure Functions MCP server | 8120 |

This README is intentionally brief. Use it for the project grid and demo
outline. UI pages, styling stacks, theme, architecture constraints, project
relationships, and parity guidance live in
[`docs/architecture.md`](docs/architecture.md).

## Out of Scope

- **Authentication**: Login/identity provider (e.g. Auth0)
- **Per-user custom map pins**: Map pins scoped to individual user accounts
- **Per-pin photo uploads**: Image upload/download per pin via blob storage SAS URIs
- **Infrastructure as Code**: Provisioning Azure resources via Bicep
- **Foundry config as code**: Managing Foundry models, tools, and agents via Azure Developer CLI (azd) templates
