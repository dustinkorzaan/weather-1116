# Weather

Weather forecast sample app implemented across four runnable stacks plus one
shared .NET class library.

This README is intentionally brief. Use it for quick orientation, and use
[`docs/architecture.md`](docs/architecture.md) for architecture constraints,
project relationships, and parity guidance.

| Project | Path | Stack |
| --- | --- | --- |
| MVC UI | [`mvc-dotnet/WeatherMVC`](mvc-dotnet/WeatherMVC) | ASP.NET Core MVC |
| API | [`api-dotnet/WeatherAPI`](api-dotnet/WeatherAPI) | ASP.NET Core Minimal API |
| React UI | [`ui-react`](ui-react) | React + Vite |
| Blazor UI | [`ui-blazor/WeatherBlazor`](ui-blazor/WeatherBlazor) | Blazor Server |
| Core | [`core-dotnet/Core.csproj`](core-dotnet/Core.csproj) | Shared .NET class library referenced by MVC and API |

Architecture reference: [`docs/architecture.md`](docs/architecture.md)
