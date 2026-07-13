# AGENTS.md

## Cursor Cloud specific instructions

This repo is one Weather sample implemented as five .NET/JS projects (see
`README.md` and `docs/architecture.md`). Four are runnable apps plus one shared
`Core` class library.

### Toolchain (already provisioned in the VM snapshot)

- .NET SDK 10 lives in `~/.dotnet` (installed via the official `dotnet-install.sh`,
  not apt). `~/.bashrc` puts it on `PATH` and sets `DOTNET_ROOT`, so interactive
  shells get `dotnet` automatically. In non-interactive contexts, call
  `"$HOME/.dotnet/dotnet"` directly.
- Node (with `npm`) is provided via nvm. React deps install into `ui-react`.
- The update script runs `npm --prefix ui-react ci` and `dotnet restore Weather.sln`;
  it intentionally does NOT install the SDK (that is snapshot state).

### Services, ports, and how to run

Run each app from its project dir with `dotnet run` (or `dotnet watch run` for
hot reload); React uses `npm start`. Ports come from each project's
`launchSettings.json` / `package.json`:

| Service | Path | Run command | Port |
| --- | --- | --- | --- |
| Weather API | `api-dotnet/WeatherAPI` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8080 |
| Weather Blazor | `ui-blazor/WeatherBlazor` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8090 |
| Weather MVC | `mvc-dotnet/WeatherMVC` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8100 |
| React UI | `ui-react` | `npm start` | 3000 |

### Non-obvious caveats

- Start `WeatherAPI` (8080) FIRST. Both the React UI (Vite proxies `/Home` and
 `/weatherforecast` to `http://localhost:8080`, override with `VITE_API_DOTNET_URL`) and
 the Blazor UI (`API_DOTNET_URL` in `ui-blazor/WeatherBlazor/appsettings.json`)
 depend on it. Without the API, React shows "Unable to load hello message" and
 Blazor's forecast/hello calls fail.
- `WeatherMVC` is standalone (duplicates backend logic via `Core`/MediatR) and
  does not call the API.
- The apps listen on plain HTTP only (no HTTPS profile). `UseHttpsRedirection`
  logs a harmless "failed to determine the https port" warning — ignore it.
- Google Maps (city pins on all three UIs) needs a browser API key with
  **Maps JavaScript API** enabled. Set:
  - React: `VITE_GOOGLE_MAPS_API_KEY` (see `ui-react/.env.example`)
  - Blazor / MVC: `GoogleMaps:ApiKey` or env `GoogleMaps__ApiKey`
  Without a key the UIs still run; the map section shows a setup hint.

### Lint / test / build

- Build everything: `dotnet build Weather.sln` (CI in
  `.github/workflows/build-and-test.yml` builds each `.csproj` in Release +
  `npm ci && npm run build && npm test -- --run` in `ui-react`).
- React: `npm run build`, and `npm test -- --run` (Vitest).
- There is no separate .NET test project.
- The four `prod-deploy-*.yml` workflows only deploy when `Build and Test`
  completes successfully on `main` (or via manual `workflow_dispatch` on `main`).
