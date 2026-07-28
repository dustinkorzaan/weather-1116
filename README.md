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
| Worker | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire servers + dashboard | 8130 |
| Core | [`core-dotnet/core`](core-dotnet/core) | Shared .NET class library referenced by MVC, API, worker, and MCP hosts | — |
| MCP DotNet | [`mcp-dotnet/mcp`](mcp-dotnet/mcp) | ASP.NET Core MCP | 8110 |
| MCP Function | [`mcp-function/mcp`](mcp-function/mcp) | Azure Functions MCP | 8120 |

Architecture reference: [`docs/architecture.md`](docs/architecture.md)

## Background worker (Hangfire)

[`worker-dotnet/worker`](worker-dotnet/worker) is the only host that runs Hangfire job servers.
API and MVC register Hangfire client storage (shared `DB_CONNECTION_STRING`) so
they can enqueue jobs later; the worker processes them.

| Project | Path | Role | Port | Endpoint |
| --- | --- | --- | --- | --- |
| Worker DotNet | [`worker-dotnet/worker`](worker-dotnet/worker) | Hangfire servers + dashboard | 8130 | `/hangfire` (POC — no auth), `/About` |

Without `DB_CONNECTION_STRING`, each app falls back to in-memory Hangfire
storage (jobs do not cross processes locally). In production, set
`DB_CONNECTION_STRING` to the same Azure SQL connection string on the worker,
API, and MVC.

Prod app: `weather1116-prod-worker` (see `prod-deploy-worker-app-service.yml`).
API and MVC probe the worker via `WORKER_DOTNET_URL` in their About trees.

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
against Core weather data (V1–V3) or a hosted Foundry Agent (V4). In
`Weather.sln` and CI, but not a production deployable; run from VS Code or
`dotnet run` in each folder.
See each `Program.cs` for required `AZURE_FOUNDRY_PROD_EUS2_*` settings.

| Project | Path | Pattern |
| --- | --- | --- |
| V1 | [`FoundryConsoleV1`](FoundryConsoleV1) (`FoundryConsoleV1ModelDirectLegacy.csproj`) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`FoundryConsoleV2`](FoundryConsoleV2) (`FoundryConsoleV2ModelDirectUnifiedAI.csproj`) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`FoundryConsoleV3`](FoundryConsoleV3) (`FoundryConsoleV3InjectFunctions.csproj`) | Injected function tools (`GetLatLongData`, `GetPublicWeatherData`) handled in-process |
| V4 | [`FoundryConsoleV4`](FoundryConsoleV4) (`FoundryConsoleV4MCP.csproj`) | Calls hosted Foundry Agent `wx1116-agent-default` (agent uses MCP lat/long + weather tools) |

**V4 settings** (same `AZURE_FOUNDRY_PROD_EUS2_*` prefix as V1–V3):

| Variable | Required | Purpose |
| --- | --- | --- |
| `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL` | Yes | Foundry project URL, e.g. `https://wx1116-prd-res-eu2.services.ai.azure.com/api/projects/wx1116-prd-prj-eu2` |
| `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME` | No | Defaults to `wx1116-agent-default` (project default version) |
| `AZURE_FOUNDRY_PROD_EUS2_KEY` | Yes | Same API key as V1–V3 |

VS Code launch configs: **Foundry Console V1** … **V4**.
