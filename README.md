# Weather

Weather sample app implemented across seven runnable stacks plus one
shared .NET class library.

This README is intentionally brief. Use it for quick orientation, and use
[`docs/architecture.md`](docs/architecture.md) for architecture constraints,
project relationships, and parity guidance.

| Project | Path | Stack | Port |
| --- | --- | --- | --- |
| Blazor UI | [`ui-blazor/blazor`](ui-blazor/blazor) | Blazor | 8090 |
| React UI | [`ui-react`](ui-react) | React + Vite | 3000 |
| MVC UI | [`mvc-dotnet/mvc`](mvc-dotnet/mvc) | ASP.NET Core MVC | 8100 |
| API | [`api-dotnet/api`](api-dotnet/api) | ASP.NET Core Minimal API | 8080 |
| Core | [`core-dotnet/core`](core-dotnet/core) | Shared .NET class library referenced by MVC, API, worker, and MCP hosts | — |
| MCP DotNet | [`mcp-dotnet/mcp`](mcp-dotnet/mcp) | ASP.NET Core MCP | 8110 |
| MCP Function | [`mcp-function/mcp`](mcp-function/mcp) | Azure Functions MCP | 8120 |
| Worker DotNet | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire servers + dashboard | 8130 |

Architecture reference: [`docs/architecture.md`](docs/architecture.md)

## Google Maps (map on all three UIs)

Each UI shows a dark-styled Google Map with sample city pins (New York, Toronto,
Atlanta, Charlotte). Weather overlays will come later.

**API to enable:** [Maps JavaScript API](https://console.cloud.google.com/google/maps-apis/api-list)
in a Google Cloud project.

**API key:** Create a browser key in Google Cloud Console → APIs & Services →
Credentials. Restrict it by HTTP referrer (e.g. `http://localhost:3000/*`,
`http://localhost:8090/*`, `http://localhost:8100/*`, plus your prod hosts).

| UI | Config |
| --- | --- |
| React | `VITE_GOOGLE_MAPS_API_KEY` in `ui-react/.env.local` (see `ui-react/.env.example`) |
| Blazor | `GOOGLE_MAPS_API_KEY` in `ui-blazor/blazor/appsettings.json`, or env `GOOGLE_MAPS_API_KEY` (see `ui-blazor/blazor/.env.example`) |
| MVC | `GOOGLE_MAPS_API_KEY` in `mvc-dotnet/mvc/appsettings.json`, or env `GOOGLE_MAPS_API_KEY` (see `mvc-dotnet/mvc/.env.example`) |

Without a key, the map container still renders and each UI shows a short setup hint.

## Foundry console demos

Local console apps that exercise Microsoft Foundry / Azure OpenAI patterns
against Core weather data (V1–V4) or a hosted Foundry Agent (V5). In
`Weather.sln` and CI, but not a production deployable; run from VS Code or
`dotnet run` in each folder.
See each `Program.cs` and `.env.example` for required settings (`AZURE_FOUNDRY_PROD_EUS2_*`, plus MCP keys for V4 and API/MVC).

| Project | Path | Pattern |
| --- | --- | --- |
| V1 | [`FoundryConsoleV1`](FoundryConsoleV1) (`FoundryConsoleV1ModelDirectLegacy.csproj`) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`FoundryConsoleV2`](FoundryConsoleV2) (`FoundryConsoleV2ModelDirectUnifiedAI.csproj`) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`FoundryConsoleV3`](FoundryConsoleV3) (`FoundryConsoleV3InjectFunctions.csproj`) | In-process tool callbacks (`GetLatLongData`, `GetPublicWeatherData`) answered by the console |
| V4 | [`FoundryConsoleV4`](FoundryConsoleV4) (`FoundryConsoleV4MCP.csproj`) | Model-direct via `ResponsesClient`, tools target remote MCP servers instead of in-process callbacks |
| V5 | [`FoundryConsoleV5`](FoundryConsoleV5) (`FoundryConsoleV5Agent.csproj`) | Hosted Foundry Agent owns instructions, response schema, and MCP tools; console sends only the user prompt |

**V4 settings** (in addition to `AZURE_FOUNDRY_PROD_EUS2_KEY`):

| Variable | Required | Purpose |
| --- | --- | --- |
| `MCP_FUNCTION_KEY` | Yes | `mcp_extension` system key for the `MyMCPFunction` server (`x-functions-key`) |
| `MCP_APP_KEY` | Yes | Bearer token for the `MyMCPApp` server |

**API/MVC AI weather settings** (same pattern as V4):

| Variable | Required | Purpose |
| --- | --- | --- |
| `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL` | Yes | Foundry project URL or OpenAI endpoint URL (e.g. `.../api/projects/{id}` or `.../openai/v1`; handler appends `/openai/v1` when missing) |
| `AZURE_FOUNDRY_PROD_EUS2_KEY` | Yes | Azure AI Foundry API key |
| `AZURE_FOUNDRY_PROD_EUS2_MODEL` | Yes | Hosted model deployment name (e.g. `gpt-5.4-mini`) |
| `MCP_FUNCTION_KEY` | Yes | `mcp_extension` system key for the `MyMCPFunction` server (`x-functions-key`) |
| `MCP_APP_KEY` | Yes | Bearer token for the `MyMCPApp` server |
| `MCP_APP_URL` | Yes | Base URL for MCP DotNet (e.g. `http://localhost:8110`) |
| `MCP_FUNCTION_URL` | Yes | Base URL for MCP Function (e.g. `http://localhost:8120`) |

**V5 settings** (agent-hosted demo only):

| Variable | Required | Purpose |
| --- | --- | --- |
| `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL` | Yes | Foundry project URL, e.g. `https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2` |
| `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME` | No | Defaults to `wx1116-agent-default` (project default version) |
| `AZURE_FOUNDRY_PROD_EUS2_KEY` | Yes | Same API key as V1–V3 |

VS Code launch configs: **Foundry Console V1** … **V5**.
