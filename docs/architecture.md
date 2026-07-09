# Site Architecture

## Purpose

This repository contains one Weather sample implemented as five projects: four
runnable applications plus one shared .NET class library. The goal is feature
parity across all UI implementations while keeping each project idiomatic for
its framework.

## Projects

| # | Project | Path | Stack | Role |
| - | --- | --- | --- | --- |
| 1 | MVC UI | [`mvc-dotnet/WeatherMVC`](../mvc-dotnet/WeatherMVC) | ASP.NET Core MVC | Server-rendered web UI |
| 2 | API | [`api-dotnet/WeatherAPI`](../api-dotnet/WeatherAPI) | ASP.NET Core Minimal API | `/weatherforecast` JSON endpoint consumed by React and Blazor UI |
| 3 | React UI | [`ui-react`](../ui-react) | React + Vite | Client-rendered single-page app |
| 4 | Blazor UI | [`ui-blazor/WeatherBlazor`](../ui-blazor/WeatherBlazor) | Blazor Server | Interactive server-rendered UI in C# |
| 5 | Core | [`core-dotnet/Core.csproj`](../core-dotnet/Core.csproj) | .NET class library | Shared events/handlers referenced by MVC and API |

## Runtime Model

- `WeatherAPI` provides forecast data for React UI and Blazor UI.
- React UI and Blazor UI consume `WeatherAPI`.
- MVC UI does not consume `WeatherAPI`.
- Backend logic is intentionally duplicated in MVC and API (no shared backend
  dependency between those projects), except for shared cross-cutting code
  (events/handlers) provided by `Core`, which both MVC and API reference.

## Core Project

`Core` (`core-dotnet/Core.csproj`) is a .NET class library referenced by both
`WeatherMVC` and `WeatherAPI`. It hosts shared demo events and handlers:

- `core-dotnet/demo/events` — event contracts (e.g. `HelloWorldEvent`).
- `core-dotnet/demo/handlers` — handlers that process events and return a
  response (e.g. `HelloWorldHandler`, `HelloWorldResponse`).

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

## Local Run Model

The four runnable applications are intended to run together in VS Code via Run
and Debug "Run All", using `.vscode/launch.json` and port forwarding
configured in `.devcontainer/devcontainer.json`.

## Build and CI

The workflow [`build.yml`](../.github/workflows/build.yml) builds all projects
on pull requests to `main`:

- `Core.csproj`, `WeatherAPI.csproj`, `WeatherBlazor.csproj`, and
  `WeatherMVC.csproj` via `dotnet build`.
- React app in `ui-react` via `npm ci && npm run build`.

## Repository Layout

```text
api-dotnet/WeatherAPI/       API project
mvc-dotnet/WeatherMVC/       MVC UI project
ui-blazor/WeatherBlazor/     Blazor UI project
ui-react/                    React UI project
core-dotnet/                 Core shared class library (Core.csproj)
docs/                        Documentation (including this file)
```
