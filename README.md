# Weather

Weather sample app implemented across seven runnable stacks plus one
shared .NET class library.

This README is intentionally brief. Use it for the project grid and demo
outline. UI pages, styling stacks, theme, architecture constraints, project
relationships, and parity guidance live in
[`docs/architecture.md`](docs/architecture.md).

| PROD | Project | Path | Stack | Port |
| --- | --- | --- | --- | --- |
| [Visit](https://weather1116-prod-blazor.azurewebsites.net) | Blazor UI | [`ui-blazor/blazor`](ui-blazor/blazor) | Blazor | 8090 |
| [Visit](https://nice-glacier-08fd44e1e.7.azurestaticapps.net) | React UI | [`ui-react`](ui-react) | React + Vite | 3000 |
| [Visit](https://weather1116-prod-mvc.azurewebsites.net) | MVC UI | [`mvc-dotnet/mvc`](mvc-dotnet/mvc) | ASP.NET Core MVC | 8100 |
|  | API | [`api-dotnet/api`](api-dotnet/api) | ASP.NET Core Minimal API | 8080 |
|  | Core | [`core-dotnet/core`](core-dotnet/core) | Shared .NET class library referenced by MVC, API, worker, and MCP hosts |  |
|  | Worker DotNet | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire dashboard and servers | 8130 |
|  | MCP Server on App Service | [`mcp-srv-app-service/mcp`](mcp-srv-app-service/mcp) | ASP.NET Core MCP server | 8110 |
|  | MCP Server on Functions App | [`mcp-srv-func-app/mcp`](mcp-srv-func-app/mcp) | Azure Functions MCP server | 8120 |

## Foundry console demos

Presentation reference: [`docs/presentation.md`](docs/presentation.md)

Local console apps that exercise Microsoft Foundry / Azure OpenAI patterns
against Core weather data (V1–V4) or a hosted Foundry Agent (V5).

| Project | Path | Pattern |
| --- | --- | --- |
| V1 | [`Foundry Console V1 Model Direct Legacy`](FoundryConsoleV1) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`Foundry Console V2 Model Direct Unified AI`](FoundryConsoleV2) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`Foundry Console V3 In Process Tool Callbacks`](FoundryConsoleV3) | In-process tool callbacks answered by the console |
| V4 | [`Foundry Console V4 MCP`](FoundryConsoleV4) | Model-direct, tools target remote MCP servers instead of in-process callbacks |
| V5 | [`Foundry Console V5 Agent`](FoundryConsoleV5) | Hosted Foundry Agent owns instructions, response schema, and MCP tools; console sends only the user prompt |

## Chat clients

Presentation reference: [`docs/5-chat-clients/5-chat-clients.md`](docs/5-chat-clients/5-chat-clients.md)

Standalone multi-turn chat page with four tabs:

| Tab | Pattern | Stack | Notes |
| --- | --- | --- | --- |
| Chat1a | In-process | Responses API | Like Foundry Console V3 |
| Chat1b | MCP | Responses API | Like Foundry Console V4 |
| Chat2a | In-process | Agent Framework | Like Foundry Console V3 |
| Chat2b | MCP | Agent Framework | Like Foundry Console V4 |
