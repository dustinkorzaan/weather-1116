# AGENTS.md

## Cursor Cloud specific instructions

This repo is one Weather sample implemented as six .NET/JS projects (see
`README.md` and `docs/architecture.md`). Five are runnable apps plus one shared
`Core` class library.

## Git / PR policy

- Create and update pull requests only.
- **Never** merge, squash-merge, or rebase-merge pull requests.
- **Never** run `gh pr merge` or equivalent.
- The user merges pull requests manually.

### Toolchain (already provisioned in the VM snapshot)

- .NET SDK 10 lives in `~/.dotnet` (installed via the official `dotnet-install.sh`,
  not apt). `~/.bashrc` puts it on `PATH` and sets `DOTNET_ROOT`, so interactive
  shells get `dotnet` automatically. In non-interactive contexts, call
  `"$HOME/.dotnet/dotnet"` directly.
- Node (with `npm`) is provided via nvm. React deps install into `ui-react`.
  Pin: `ui-react/.nvmrc` and `package.json` `engines` require **Node >=24** and **npm >=11**.
- The update script runs `npm --prefix ui-react ci` and `dotnet restore Weather.sln`;
  it intentionally does NOT install the SDK (that is snapshot state).

### Services, ports, and how to run

Run each app from its project dir with `dotnet run` (or `dotnet watch run` for
hot reload); React uses `npm start`. Ports come from each project's
`launchSettings.json` / `package.json`:

| Service | Path | Run command | Port |
| --- | --- | --- | --- |
| Weather API | `api-dotnet/api` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8080 |
| Weather Blazor | `ui-blazor/blazor` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8090 |
| Weather MVC | `mvc-dotnet/mvc` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8100 |
| React UI | `ui-react` | `npm start` | 3000 |
| Worker DotNet | `worker-dotnet/worker` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8130 |
| MCP Server on App Service | `mcp-srv-app-service/mcp` | `ASPNETCORE_ENVIRONMENT=Development dotnet run` | 8110 |
| MCP Server on Functions App | `mcp-srv-func-app/mcp` | `func start` from `mcp-srv-func-app/mcp` (or VS Code **WeatherMcpSrvFuncApp**) | 8120 |

### Non-obvious caveats

- Start `WeatherAPI` (8080) FIRST. Both the React UI (Vite proxies `/Home` and
 `/AIWeather` to `http://localhost:8080`, override with `VITE_API_DOTNET_URL`) and
 the Blazor UI (`API_DOTNET_URL` in `ui-blazor/blazor/appsettings.json`)
 depend on it. Without the API, React shows "Unable to load hello message" and
 Blazor's hello call fails.
- `WeatherMVC` is standalone (duplicates backend logic via `Core`/MediatR) and
  does not call the API.
- `worker-dotnet` runs Hangfire job servers and exposes `/hangfire` (dashboard,
  POC — no auth) and `/About`. API and MVC are Hangfire clients only (shared
  `DB_CONNECTION_STRING` storage); without a DB connection string each process
  falls back to its own in-memory storage, so jobs do not cross apps locally.
- The apps listen on plain HTTP only (no HTTPS profile). `UseHttpsRedirection`
  logs a harmless "failed to determine the https port" warning — ignore it.
- Google Maps (city pins on all three UIs) needs a browser API key with
  **Maps JavaScript API** enabled. Set:
  - React: `VITE_GOOGLE_MAPS_API_KEY` (see `ui-react/.env.example`)
  - Blazor / MVC: `GOOGLE_MAPS_API_KEY` or env `GOOGLE_MAPS_API_KEY`
  Without a key the UIs still run; the map section shows a setup hint.

### Screenshots and videos

Do **not** record screen videos, take walkthrough screenshots, or drive the
UI with computer-use unless the user explicitly asks for that, or asks you
to review a pull request and visual evidence is needed for that review.

Prefer automated tests and command output. Skip GUI walkthrough artifacts
for ordinary implementation work.

### Lint / test / build

- Build everything: `dotnet build Weather.sln` (CI in
  `.github/workflows/build-and-test.yml` builds each `.csproj` in Release +
  `npm ci && npm run build && npm test -- --run` in `ui-react`).
- React: `npm run build`, and `npm test -- --run` (Vitest).
- .NET test projects: `core-dotnet/core.tests`, `api-dotnet/api.tests`,
  `mvc-dotnet/mvc.tests`, `worker-dotnet/worker.tests`, `ui-blazor/blazor.tests`,
  `mcp-srv-app-service/mcp.tests`, and `mcp-srv-func-app/mcp.tests` (see CI
  `build-and-test.yml`).
- The `prod-deploy-*.yml` workflows auto-deploy when `build-and-test` completes
  successfully on `main` (e.g. after a merged PR). Each workflow can also be run
  manually via `workflow_dispatch` on any branch (e.g. hotfixes).
