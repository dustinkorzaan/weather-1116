# Weather

Weather sample app implemented across seven runnable stacks plus one
shared .NET class library.

This README is intentionally brief. Use it for quick orientation, and use
[`docs/architecture.md`](docs/architecture.md) for architecture constraints,
project relationships, and parity guidance.

| Project | Path | Stack | Port | PROD |
| --- | --- | --- | --- | --- |
| Blazor UI | [`ui-blazor/blazor`](ui-blazor/blazor) | Blazor | 8090 | <a href="https://weather1116-prod-blazor.azurewebsites.net" target="_blank" rel="noopener noreferrer">Blazor</a> |
| React UI | [`ui-react`](ui-react) | React + Vite | 3000 | <a href="https://nice-glacier-08fd44e1e.7.azurestaticapps.net" target="_blank" rel="noopener noreferrer">React</a> |
| MVC UI | [`mvc-dotnet/mvc`](mvc-dotnet/mvc) | ASP.NET Core MVC | 8100 | <a href="https://weather1116-prod-mvc.azurewebsites.net" target="_blank" rel="noopener noreferrer">MVC</a> |
| API | [`api-dotnet/api`](api-dotnet/api) | ASP.NET Core Minimal API | 8080 | — |
| Core | [`core-dotnet/core`](core-dotnet/core) | Shared .NET class library referenced by MVC, API, worker, and MCP hosts | — | — |
| MCP Server on App Service | [`mcp-srv-app-service/mcp`](mcp-srv-app-service/mcp) | ASP.NET Core MCP server | 8110 | — |
| MCP Server on Functions App | [`mcp-srv-func-app/mcp`](mcp-srv-func-app/mcp) | Azure Functions MCP server | 8120 | — |
| Worker DotNet | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire servers + dashboard | 8130 | — |

Architecture reference: [`docs/architecture.md`](docs/architecture.md)

## UI pages and styling

All three UIs implement the same pages (behavioral parity), each styled with its own
framework-native library (no shared CSS, config, or components):

| Route | Contents |
| --- | --- |
| `/` | Top bar (logo left, person menu right) above a full-viewport Google Map |
| `/hello-world` | Same top bar, then the hello message |
| `/current-ai-weather` | Same top bar, then the Current AI Weather widget |
| `/chat-clients` | Same top bar, then the chat clients |

| UI | Styling |
| --- | --- |
| React | Tailwind CSS v4 (`@tailwindcss/vite`) + shadcn/ui (Radix) + lucide-react |
| Blazor | Fluent UI Blazor |
| MVC | Hand-written CSS + vanilla JS ([`mvc-dotnet/README.md`](mvc-dotnet/README.md)) |

## Foundry console demos

Presentation reference: [`docs/presentation.md`](docs/presentation.md)

Local console apps that exercise Microsoft Foundry / Azure OpenAI patterns
against Core weather data (V1–V4) or a hosted Foundry Agent (V5). In
`Weather.sln` and CI, but not a production deployable; run from VS Code or
`dotnet run` in each folder.
See each `Program.cs` and `.env.example` for required settings (`AZURE_FOUNDRY_PROD_EUS2_*`, plus MCP keys for V4 and API/MVC).

| Project | Path | Pattern |
| --- | --- | --- |
| V1 | [`FoundryConsoleV1`](FoundryConsoleV1) (`FoundryConsoleV1ModelDirectLegacy.csproj`) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`FoundryConsoleV2`](FoundryConsoleV2) (`FoundryConsoleV2ModelDirectUnifiedAI.csproj`) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`FoundryConsoleV3`](FoundryConsoleV3) (`FoundryConsoleV3InProcessToolCallbacks.csproj`) | In-process tool callbacks (`GetLatLongData`, `GetPublicWeatherData`) answered by the console |
| V4 | [`FoundryConsoleV4`](FoundryConsoleV4) (`FoundryConsoleV4MCP.csproj`) | Model-direct via `ResponsesClient`, tools target remote MCP servers instead of in-process callbacks |
| V5 | [`FoundryConsoleV5`](FoundryConsoleV5) (`FoundryConsoleV5Agent.csproj`) | Hosted Foundry Agent owns instructions, response schema, and MCP tools; console sends only the user prompt |

VS Code launch configs: **Foundry Console V1** … **V5**.

## Chat clients

Presentation reference: [`docs/5-chat-clients/5-chat-clients.md`](docs/5-chat-clients/5-chat-clients.md)

Standalone multi-turn chat on the `/chat-clients` page of React, Blazor, and MVC with four tabs:
**Chat1a** (Responses + in-process), **Chat1b** (Responses + MCP), **Chat2a** (Agent Framework +
in-process), **Chat2b** (Agent Framework + MCP). Separate from the Current AI Weather widget on
the same page.
