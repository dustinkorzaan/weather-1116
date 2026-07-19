# Weather

Weather forecast sample app implemented across four runnable UI/API stacks, two
MCP tool hosts, plus one shared .NET class library.

This README is intentionally brief. Use it for quick orientation, and use
[`docs/architecture.md`](docs/architecture.md) for architecture constraints,
project relationships, and parity guidance.

| Project | Path | Stack | Port |
| --- | --- | --- | --- |
| MVC UI | [`mvc-dotnet/WeatherMVC`](mvc-dotnet/WeatherMVC) | ASP.NET Core MVC | 8100 |
| API | [`api-dotnet/WeatherAPI`](api-dotnet/WeatherAPI) | ASP.NET Core Minimal API | 8080 |
| React UI | [`ui-react`](ui-react) | React + Vite | 3000 |
| Blazor UI | [`ui-blazor/WeatherBlazor`](ui-blazor/WeatherBlazor) | Blazor Server | 8090 |
| MCP DotNet | [`mcp-dotnet`](mcp-dotnet) | ASP.NET Core MCP server (`GetPublicWeatherData`) | 8110 |
| MCP Function | [`mcp-function`](mcp-function) | Azure Functions MCP server (`GetLatLongData`) | 8120 |
| Core | [`core-dotnet/Core.csproj`](core-dotnet/Core.csproj) | Shared .NET class library referenced by MVC, API, and MCP hosts | — |

Both MCP hosts call Core via MediatR. Local/Codespaces ports are also listed in
[`.devcontainer/devcontainer.json`](.devcontainer/devcontainer.json) for
forwarding. VS Code launch configs: **WeatherMcpDotNet**, **WeatherMcpFunction**.

Architecture reference: [`docs/architecture.md`](docs/architecture.md)

## Google Maps (city map on all three UIs)

Each UI shows a dark-styled Google Map with sample city pins (New York, Toronto,
Atlanta, Charlotte). Weather overlays will come later.

**API to enable:** [Maps JavaScript API](https://console.cloud.google.com/google/maps-apis/api-list)
in a Google Cloud project.

**API key:** Create a browser key in Google Cloud Console → APIs & Services →
Credentials. Restrict it by HTTP referrer (e.g. `http://localhost:3000/*`,
`http://localhost:8090/*`, `http://localhost:8100/*`, plus your prod hosts).

| UI | Config |
| --- | --- |
| React | `VITE_GOOGLE_MAPS_API_KEY` in `ui-react/.env.local` (see `.env.example`) |
| Blazor | `GoogleMaps:ApiKey` in `appsettings.json`, or env `GoogleMaps__ApiKey` |
| MVC | `GoogleMaps:ApiKey` in `appsettings.json`, or env `GoogleMaps__ApiKey` |

Without a key, the map container still renders and each UI shows a short setup hint.
