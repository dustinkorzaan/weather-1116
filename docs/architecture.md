# Site Architecture

This repository hosts a single "Weather" sample built four separate times,
once for each of four project types. The intent is **feature parity**: each
project should let a user view a 5-day weather forecast, using the idioms and
conventions native to its own stack.

## Projects

| # | Project | Path | Stack | Role |
| - | --- | --- | --- | --- |
| 1 | MVC | [`mvc-dotnet/WeatherMVC`](../mvc-dotnet/WeatherMVC) | ASP.NET Core MVC (Razor views, controllers) | Server-rendered web UI |
| 2 | API | [`api-dotnet/WeatherAPI`](../api-dotnet/WeatherAPI) | ASP.NET Core Minimal API | `/weatherforecast` JSON endpoint consumed by the UI projects |
| 3 | React UI | [`ui-react`](../ui-react) | React + Vite | Client-side rendered single-page app |
| 4 | Blazor UI | [`ui-blazor/WeatherBlazor`](../ui-blazor/WeatherBlazor) | Blazor Server | Server-rendered interactive UI (C# instead of JavaScript) |

All three .NET projects are wired together in [`Weather.sln`](../Weather.sln).

## Feature Parity Goal

Each of the four projects should ultimately expose the same core feature:

- Display a 5-day weather forecast (date, temperature in °C/°F, and a summary)
  in a table.
- Match the same site branding (logo, title, layout) across projects.

Individual projects may consume `WeatherAPI` directly (as `WeatherBlazor`
does via `WeatherForecastClient`) or generate their own sample data locally,
as long as the presented feature set stays equivalent across all four.

## Build & CI

The [`build.yml`](../.github/workflows/build.yml) workflow builds all four
projects on every pull request to `main`:

- `WeatherAPI.csproj`, `WeatherBlazor.csproj`, and `WeatherMVC.csproj` are
  built via `dotnet build`.
- `ui-react` is built via `npm ci && npm run build`.

## Repository Layout

```
api-dotnet/WeatherAPI/       # API project
mvc-dotnet/WeatherMVC/       # MVC project
ui-blazor/WeatherBlazor/     # Blazor UI project
ui-react/                    # React UI project
Weather.sln                  # Solution file linking the .NET projects
docs/                        # Documentation (this file, session notes, etc.)
```
