# Weather MVC

ASP.NET Core MVC UI (port 8100). Standalone: it duplicates backend logic through
`Core`/MediatR instead of calling the Weather API.

Run it:

```bash
cd mvc-dotnet/mvc
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Pages: `/` (top bar + full-viewport Google Map) and `/presentation` (hello
message, Current AI Weather widget, chat clients).

## Styling: Tailwind CSS via the standalone CLI

The MVC project has **no Node dependency**. Styling is Tailwind CSS v4 compiled
with the [standalone Tailwind CLI](https://tailwindcss.com/blog/standalone-cli)
binary; interactive behavior (avatar dropdown, About modal) is vanilla JS in
`mvc/wwwroot/js/site.js`. Bootstrap is not used.

- Source: `mvc/Styles/app.css` (`@import "tailwindcss";` plus `@source` globs for
  `Views/**/*.cshtml` and `wwwroot/js/**/*.js`, so classes used in markup and JS
  survive purging).
- Compiled output (committed, referenced by `_Layout.cshtml`):
  `mvc/wwwroot/css/site.css`.

Install the CLI once (Linux x64 shown; see the release page for other platforms):

```bash
curl -sLo /usr/local/bin/tailwindcss \
  https://github.com/tailwindlabs/tailwindcss/releases/download/v4.3.3/tailwindcss-linux-x64
chmod +x /usr/local/bin/tailwindcss
```

Watch during development (run from `mvc-dotnet/mvc`):

```bash
tailwindcss -i Styles/app.css -o wwwroot/css/site.css --watch
```

One-off production build (commit the result when classes change):

```bash
tailwindcss -i Styles/app.css -o wwwroot/css/site.css --minify
```

## Tests

```bash
dotnet test mvc-dotnet/mvc.tests/WeatherMVC.Tests.csproj
```
