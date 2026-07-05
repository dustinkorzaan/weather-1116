# Weather

A weather forecast sample implemented four times, once per stack, so the same
feature set can be compared side by side across different technologies.

| Project | Path | Stack |
| --- | --- | --- |
| MVC | [`mvc-dotnet/WeatherMVC`](mvc-dotnet/WeatherMVC) | ASP.NET Core MVC (Razor views) |
| API | [`api-dotnet/WeatherAPI`](api-dotnet/WeatherAPI) | ASP.NET Core Minimal API |
| React UI | [`ui-react`](ui-react) | React (Vite) |
| Blazor UI | [`ui-blazor/WeatherBlazor`](ui-blazor/WeatherBlazor) | Blazor Server |

See [`docs/architecture.md`](docs/architecture.md) for details on how the
projects relate and the feature-parity goals across them.
