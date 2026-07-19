# Weather

Weather forecast sample app implemented across four runnable stacks plus one
shared .NET class library.

This README is intentionally brief. Use it for quick orientation, and use
[`docs/architecture.md`](docs/architecture.md) for architecture constraints,
project relationships, and parity guidance.

| Project | Path | Stack | Port |
| --- | --- | --- | --- |
| MVC UI | [`mvc-dotnet/WeatherMVC`](mvc-dotnet/WeatherMVC) | ASP.NET Core MVC | 8100 |
| API | [`api-dotnet/WeatherAPI`](api-dotnet/WeatherAPI) | ASP.NET Core Minimal API | 8080 |
| React UI | [`ui-react`](ui-react) | React + Vite | 3000 |
| Blazor UI | [`ui-blazor/WeatherBlazor`](ui-blazor/WeatherBlazor) | Blazor Server | 8090 |
| Core | [`core-dotnet/Core.csproj`](core-dotnet/Core.csproj) | Shared .NET class library referenced by MVC and API | — |

Architecture reference: [`docs/architecture.md`](docs/architecture.md)

## MCP tool hosts

Ultra-simple remote MCP servers that expose Core weather tools via MediatR.

| Project | Path | Tool | Port | Endpoint | Auth |
| --- | --- | --- | --- | --- | --- |
| MCP DotNet | [`mcp-dotnet`](mcp-dotnet) | `GetPublicWeatherData` | 8110 | `/mcp` | Bearer token (`Mcp:ApiKey` / env `Mcp__ApiKey`; Dev default `dev-mcp-dotnet-key`) |
| MCP Function | [`mcp-function`](mcp-function) | `GetLatLongData` | 8120 | `/runtime/webhooks/mcp` | Built-in Functions system key `mcp_extension` (`x-functions-key` header) |

VS Code launch configs: **WeatherMcpDotNet**, **WeatherMcpFunction**. Ports are
also forwarded in [`.devcontainer/devcontainer.json`](.devcontainer/devcontainer.json).

Prod apps: `weather1116-prod-mcpapp`, `weather1116-prod-mcpfunc` (see `prod-deploy-mcp-*.yml`).

Examples:
- MCP DotNet: `Authorization: Bearer dev-mcp-dotnet-key` (`/health` stays open)
- MCP Function (Azure): `x-functions-key: {mcp_extension system key from App keys}`

## Foundry console demos

Local console apps that exercise Microsoft Foundry / Azure OpenAI patterns
against Core weather data. Not part of `Weather.sln` deployables; run from VS Code
or `dotnet run` in each folder. Expect a Foundry API key in the environment
(see each `Program.cs`).

| Project | Path | Pattern |
| --- | --- | --- |
| V1 | [`FoundryConsoleV1ModelDirectLegacyCognitiveServicesEndpoint`](FoundryConsoleV1ModelDirectLegacyCognitiveServicesEndpoint) | Model-direct via legacy `AzureOpenAIClient` / Cognitive Services endpoint |
| V2 | [`FoundryConsoleV2ModelDirectNewUnifiedAIServices`](FoundryConsoleV2ModelDirectNewUnifiedAIServices) | Model-direct via `ResponsesClient` against the unified AI services endpoint |
| V3 | [`FoundryConsoleV3InjectFunctions`](FoundryConsoleV3InjectFunctions) | Injected function tools (`GetLatLongData`, `GetPublicWeatherData`) handled in-process |
| V4 | [`FoundryConsoleV4MCP`](FoundryConsoleV4MCP) | Model-direct JSON in / JSON out (named MCP; remote MCP hosts are the projects above) |

VS Code launch configs: **Foundry Console V1** … **V4**.

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
