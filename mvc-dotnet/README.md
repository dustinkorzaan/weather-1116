# Weather MVC

ASP.NET Core MVC UI (port 8100). Standalone: it duplicates backend logic through
`Core`/CQMediator instead of calling the Weather API.

Run it:

```bash
cd mvc-dotnet/mvc
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Pages: `/` (top bar + full-viewport Google Map), `/hello-world`,
`/current-ai-weather`, and `/chat-clients`.

## Styling: hand-written CSS + vanilla JS

The MVC project has **no Node dependency** and **no CSS framework**. Layout and
theme tokens live in `mvc/wwwroot/css/site.css` (semantic classes, referenced by
`_Layout.cshtml`). Interactive behavior is vanilla JS:

- `mvc/wwwroot/js/site.js` — avatar dropdown, About modal
- `mvc/wwwroot/js/theme.js` — Light / Dark / System preference (`localStorage`
  key `weather-theme`)
- `mvc/wwwroot/js/weatherMap.js` — map, pins, and themed hover cards

Bootstrap, Tailwind, and component libraries are not used — `dotnet run` is the
whole frontend toolchain.

## Tests

```bash
dotnet test mvc-dotnet/mvc.tests/WeatherMVC.Tests.csproj
```
