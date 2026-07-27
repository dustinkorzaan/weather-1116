# Site Architecture

## Purpose

This repository contains one Weather sample implemented as six primary
projects: five runnable applications plus one shared .NET class library. The
goal is feature parity across all UI implementations while keeping each project
idiomatic for its framework.

The repo also includes **MCP tool hosts** and **Foundry console demos** that
are not called directly by the UIs, but they exercise the same `Core` weather
and geo logic and support the hosted **Azure Foundry agent** used for Current
AI Weather in API and MVC.

## Projects

### Deployable applications and shared library

| # | Project | Path | Stack | Role |
| - | --- | --- | --- | --- |
| 1 | MVC UI | [`mvc-dotnet/mvc`](../mvc-dotnet/mvc) | ASP.NET Core MVC | Server-rendered web UI |
| 2 | API | [`api-dotnet/api`](../api-dotnet/api) | ASP.NET Core Minimal API | JSON API consumed by React and Blazor UI |
| 3 | React UI | [`ui-react`](../ui-react) | React + Vite | Client-rendered single-page app |
| 4 | Blazor UI | [`ui-blazor/blazor`](../ui-blazor/blazor) | Blazor Server | Interactive server-rendered UI in C# |
| 5 | Core | [`core-dotnet/core/Core.csproj`](../core-dotnet/core/Core.csproj) | .NET class library | Shared events/handlers referenced by MVC, API, and MCP hosts |

### Adjacent projects (not UI/API dependencies)

These are part of the overall system map but are **not** on the critical path
for hello/AI weather/map flows in the three UIs.

| Project | Path | Role |
| --- | --- | --- |
| Worker DotNet | [`worker-dotnet/worker`](../worker-dotnet/worker) | Hangfire job servers, dashboard (`/hangfire`), and `/About` health leaf |
| MCP DotNet | [`mcp-dotnet`](../mcp-dotnet) | Remote MCP server exposing `GetPublicWeatherData` via `Core` |
| MCP Function | [`mcp-function`](../mcp-function) | Azure Functions MCP host exposing `GetLatLongData` via `Core` |
| Foundry Console V1–V4 | [`FoundryConsoleV1`](../FoundryConsoleV1) … [`V4`](../FoundryConsoleV4) | Local learning demos for Foundry / agent patterns (in `Weather.sln` as `FoundryConsoleV1ModelDirectLegacy`–`V4MCP`; built in CI) |

Ports, auth, and env vars for the worker, MCP, and console apps live in [`README.md`](../README.md)
and each project's `.env.example`.

## Runtime Model

- `WeatherAPI` provides hello and AI weather data for React UI and Blazor UI.
- React UI and Blazor UI consume `WeatherAPI`.
- MVC UI does not consume `WeatherAPI` for AI weather/hello; it duplicates the
  equivalent backend logic locally via `Core`.
- Backend logic is intentionally duplicated in MVC and API (no shared backend
  dependency between those projects), except for shared cross-cutting code
  (events/handlers) provided by `Core`, which both MVC and API reference.
- **MCP hosts are not called by any UI or by `WeatherAPI` for standard
  AI weather/hello flows.** They are used indirectly when the hosted Foundry
  agent resolves a place name and fetches weather (see below).

## AI Weather and Foundry Agent

All three UIs expose **Current AI Weather**. The request path differs by stack:

- **React / Blazor** → `WeatherAPI` (`/AIWeather/Current`)
- **MVC** → local `HomeController` + `Core` (same handler, no API hop)

Both API and MVC route AI weather through `Core.AIWeather.Handlers.GetCurrentAIWeatherHandler`,
which calls a **hosted Microsoft Foundry agent** (default `wx1116-agent-default`).
The agent is configured in Azure to use MCP tools that map back to this repo's
`Core` handlers:

```mermaid
flowchart LR
  UI[React / Blazor / MVC]
  API[MVC or WeatherAPI]
  Core[Core GetCurrentAIWeatherHandler]
  Agent[Azure Foundry Agent]
  McpFunc[mcp-function GetLatLongData]
  McpDotNet[mcp-dotnet GetPublicWeatherData]
  CoreGeo[Core geo handlers]
  CoreWx[Core weather handlers]

  UI --> API
  API --> Core
  Core --> Agent
  Agent --> McpFunc
  Agent --> McpDotNet
  McpFunc --> CoreGeo
  McpDotNet --> CoreWx
```

Required settings for API/MVC in production: `AZURE_FOUNDRY_PROD_EUS2_PROJ_URL`,
`AZURE_FOUNDRY_PROD_EUS2_KEY`, and optionally `AZURE_FOUNDRY_PROD_EUS2_AGENT_NAME`.
See deploy workflows and `.env.example` files in `api-dotnet` and `mvc-dotnet`.

## MCP Tool Hosts

Ultra-simple remote MCP servers that expose `Core` tools over the Model Context
Protocol. They exist so a Foundry project (or MCP Inspector) can call the same
MediatR handlers the sample uses in-process elsewhere.

| Host | Tool | Endpoint | Auth |
| --- | --- | --- | --- |
| MCP DotNet | `GetPublicWeatherData` | `/mcp` | Bearer `MCP_API_KEY` |
| MCP Function | `GetLatLongData` | `/runtime/webhooks/mcp` (Azure) | Functions system key `mcp_extension` |

Each host also exposes an anonymous **`/About`** probe that returns a leaf
`AboutNode` (`mcp-dotnet` or `mcp-function`) with tool-registration health and
optional `BUILD_NUMBER` / `BUILD_START` / `BUILD_BRANCH_NAME` metadata.

API and MVC `/About` aggregate those remote nodes as children under their
`API Root` subtree (see [About and health](#about-and-health)). Production base
URLs are configured via `MCP_DOTNET_URL`, `MCP_FUNCTION_URL`, and
`WORKER_DOTNET_URL` (GitHub variables `PROD_MCP_DOTNET_URL`,
`PROD_MCP_FUNCTION_URL`, `PROD_WORKER_DOTNET_URL`); `/About` is appended in code.

## Background Worker (Hangfire)

[`worker-dotnet`](../worker-dotnet) is the only app that runs Hangfire servers.
It runs queue-based Hangfire servers against shared storage
(`DB_CONNECTION_STRING`, SQL Server in production). API and MVC register the
same storage as Hangfire **clients** so they can enqueue jobs without running
servers.

- **Dashboard:** `/hangfire` on the worker (POC — open to all; auth TBD).
- **Health:** `/About` returns `Worker Root` with `worker-dotnet` and a `Hangfire`
  child. The Hangfire node exposes queue counts in `publicMessage` and marks
  itself unhealthy when failed jobs exist or jobs exceed staleness thresholds
  (default 30 minutes processing / 60 minutes enqueued; configure via
  `HangfireAboutHealth_StaleProcessingMinutes` and
  `HangfireAboutHealth_StaleEnqueuedMinutes` in `appsettings.json` or `.env`).
  API and MVC probe this tree as the `Worker Root` child.
- **Local dev:** without `DB_CONNECTION_STRING`, each process uses in-memory
  storage; jobs do not cross apps until a shared database is configured.

## Foundry Console Demos (learning path)

Four standalone console apps demonstrate how the hosted agent pattern in API/MVC
was built up. They are **training building blocks**, not production deployables:

| Console | Pattern taught |
| --- | --- |
| **V1** | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| **V2** | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| **V3** | In-process injected function tools (`GetLatLongData`, `GetPublicWeatherData`) — same tools `Core` exposes, handled locally |
| **V4** | Calls the **same hosted Foundry agent** API/MVC use; agent invokes MCP lat/long + weather tools |

Run from VS Code or `dotnet run` in each folder. Settings use the
`AZURE_FOUNDRY_PROD_EUS2_*` prefix (see [`README.md`](../README.md)).

Suggested reading order: V1 → V2 → V3 → V4 → `GetCurrentAIWeatherHandler` in
`core-dotnet/core/AIWeather`.

## About and Health

Every runnable app can participate in a shared **About tree** contract
(`Core.About.AboutNode`): `name`, `publicMessage`, `isHealthy`, build metadata,
and `children`.

## Core Project

`Core` (`core-dotnet/core/Core.csproj`) is a .NET class library referenced by
`WeatherMVC`, `WeatherAPI`, `worker-dotnet`, and both MCP hosts. It hosts shared MediatR events
and handlers, including:

- `core-dotnet/core/HelloWorld/` — hello-world demo (`HelloWorldEvent`, `HelloWorldHandler`)
- `core-dotnet/core/Geo/` — geocoding (`GetLatLongData`)
- `core-dotnet/core/Weather/` — public weather (`GetPublicWeatherData`)
- `core-dotnet/core/AIWeather/` — Foundry agent integration (`GetCurrentAIWeatherHandler`)
- `core-dotnet/core/About/` — About tree builder and remote about client

## Feature Parity Contract

For developers and AI agents, parity means the three UI projects (MVC, React,
Blazor) should remain behaviorally aligned from a user perspective.

For backend changes, keep MVC and API implementations aligned by duplicating
equivalent backend logic in both projects.

For UI changes, keep MVC, React, and Blazor implementations aligned by
duplicating equivalent UI behavior in all three projects.

It is acceptable for React and Blazor to repeat equivalent frontend
models instead of sharing code. These projects are intentionally framework-
native and independently maintainable; parity is behavioral and API-contract
based, not enforced through shared frontend model artifacts.

## Responsive Design Contract

All three UI projects (MVC, React, Blazor) are built to be responsive: each
site must render cleanly from small mobile browser widths (~320px) up through
full desktop/full-screen widths, without relying on a separate mobile-only
experience.

Shared responsive conventions kept in parity across the three sites:

- A single fluid layout adapts at breakpoints rather than branching into
  distinct mobile/desktop templates.
- Primary navigation collapses behind a toggle (hamburger) on narrow
  viewports and expands inline (MVC navbar, Blazor sidebar, React top bar) on
  wider viewports.
- The avatar/About menu stays reachable and usable at every width and never
  overflows the viewport.
- Main content is centered with a max-width on large screens instead of
  stretching to full width, keeping line lengths and table density readable.
- Base typography scales down slightly on small screens and back up at
  tablet/desktop breakpoints for comfortable readability at every size.
- The map section (Google Maps) appears on the home page of each UI with
  the same sample pins and dark styling; map height stays usable on narrow
  viewports.

## Google Maps

All three UIs embed a Maps JavaScript API map with sample city coordinates.
Configuration:

- React: `VITE_GOOGLE_MAPS_API_KEY` (build-time Vite env)
- Blazor / MVC: `GOOGLE_MAPS_API_KEY` (appsettings or `GOOGLE_MAPS_API_KEY` env)

Pins are static sample data today (ready for weather overlays later).

## Local Run Model

The four primary runnable applications are intended to run together in VS Code
via Run and Debug **Run All**, using `.vscode/launch.json` and port forwarding
in [`.devcontainer/devcontainer.json`](../.devcontainer/devcontainer.json).

MCP hosts and Foundry consoles are optional for UI development but required to
exercise the full agent + MCP path end-to-end. Start `WeatherAPI` (8080) before
React or Blazor when testing API-dependent features.

## Build and CI

The workflow [`build-and-test.yml`](../.github/workflows/build-and-test.yml)
builds on every push:

- `Core.csproj`, `WeatherAPI.csproj`, `WeatherBlazor.csproj`, `WeatherMVC.csproj`,
  `WeatherWorkerDotNet.csproj`, `WeatherMcpDotNet.csproj`, `WeatherMcpFunction.csproj`,
  and the four Foundry console projects (`FoundryConsoleV1ModelDirectLegacy`–`V4MCP`) via `dotnet build`.
- React app in `ui-react` via `npm ci && npm run build`, followed by
  `npm test -- --run` (Vitest).
- `WeatherAPI.Tests` integration tests.
- `WeatherBlazor.Tests` component tests.

Production deploy workflows (`prod-deploy-*.yml`) auto-deploy when **build-and-test**
completes successfully on `main`. Each deploy workflow can also be triggered manually
via `workflow_dispatch` on any branch. Deployables include API, MVC, React, Blazor,
worker-dotnet, and both MCP hosts.

## Repository Layout

```text
api-dotnet/
  api/                       API project (WeatherAPI.csproj)
  api.tests/                 API unit tests (WeatherAPI.Tests.csproj)
mvc-dotnet/
  mvc/                       MVC UI project (WeatherMVC.csproj)
ui-blazor/
  blazor/                    Blazor UI project (WeatherBlazor.csproj)
  blazor.tests/              Blazor unit tests (WeatherBlazor.Tests.csproj)
ui-react/                    React UI project
core-dotnet/
  core/                      Core shared class library (Core.csproj)
  core.tests/                Core unit tests (Core.Tests.csproj)
worker-dotnet/
  worker/                    Hangfire background worker (WeatherWorkerDotNet.csproj)
  worker.tests/              Worker unit tests (WeatherWorkerDotNet.Tests.csproj)
mcp-dotnet/                  MCP DotNet tool host (GetPublicWeatherData)
mcp-function/                MCP Function tool host (GetLatLongData)
FoundryConsoleV1…V4/         Foundry learning console demos
docs/                        Documentation (including this file)
```
