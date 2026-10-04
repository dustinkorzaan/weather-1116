# AGENTS.md

## Cursor Cloud specific instructions

This repo is one Weather sample implemented across nine runnable stacks plus
one shared `Core` class library (see `README.md` and `docs/architecture.md`).
Six of those projects are primary: five runnable applications (React UI,
Blazor UI, MVC UI, API, and Worker) plus the shared `Core` class library;
the other four runnable stacks are the MCP hosts (Container App, Functions
on ACA, and standalone Python and Node.js servers with no dependency on `Core`).

## Git / PR policy

- Create and update pull requests only.
- **Never** merge, squash-merge, or rebase-merge pull requests.
- **Never** run `gh pr merge` or equivalent.
- The user merges pull requests manually.

## Agent orchestration (Claude Code)

- **One check command:** `scripts/verify.sh`.
  - It maps changed files (vs `origin/main`) to the same checks CI runs and installs missing deps on demand.
  - Modes: `--all`, `--list`, and `<path>...`.
  - Use it instead of per-stack commands. "Done" means it is green.
- **Pipeline:** `/ship <story>` (`.claude/skills/ship/SKILL.md`) runs:
  1. interview → spec in `docs/specs/`
  2. `planner`
  3. parallel `implementer`s in git worktrees, plus `test-author`
  4. verify
  5. `peer-reviewer`
  6. `final-reviewer`
  7. draft PR → PR follow-through (`.claude/skills/steward/SKILL.md`)

  Each rework loop runs at most **3** rounds, then stops and reports. Flags: `--hands-off` (no questions), `--quick` (small single-stack change).
- **`/sync`** (`.claude/skills/sync/SKILL.md`) brings a story branch up to date after other PRs merge:
  - merge `origin/main` (never rebase);
  - adapt the branch to the overlapping changes;
  - verify, re-review, then push and watch CI.

  Pass `#123` or a branch name from a new thread; `--light` skips the overlap scan, and skips review unless there were conflicts or verify fixes. When several PRs are waiting, handle them one at a time: the user merges one, then `/sync` runs on the next. Agents never merge PRs.
  CI runs on branch pushes, not on the merge with `main`, so the steward also runs `/sync --light` on its own when `main` gains commits that overlap the PR (*Stale base* in the steward skill).
- **Roles** live in `.claude/agents/`. The review checklist is `REVIEW.md`; the spec template is `docs/specs/_template.md`. Peer reviews are posted on the open PR (a summary comment, plus one line comment per finding) so other agents can reply in thread. A review that stays in chat is not delivered.
- **Hooks:**
  - `SessionStart` installs every stack's toolchain and deps.
  - `Stop`/`SubagentStop` run `scripts/verify.sh` on changed files and refuse to let a writing agent finish while it's red (max 3 blocks). Set `CLAUDE_VERIFY_HOOK=off` in the environment to disable.

### Parallel work

Stack folders are independent and safe to change in parallel. These paths are **serialized**: only one worker edits them at a time, and plans order them first:

- `core-dotnet/`, `cqmediator-dotnet/`, `Weather.sln` (every .NET project depends on them)
- `infra/`, `.github/`, `.claude/`, `AGENTS.md`, `REVIEW.md`
- any MCP tool name registration (a tool name lives on exactly one host)

Parity work (see `docs/architecture.md` *Feature Parity Contract*) splits naturally: one task each for React, Blazor and MVC, all in parallel.

### Toolchain (already provisioned in the VM snapshot)

This subsection describes the pre-provisioned Cursor Cloud VM snapshot only.
Claude Code Remote sessions provision their own container instead, via
`.claude/hooks/session-start.sh`. That script installs the .NET 10 SDK, Node 24, a
Python 3.12 venv at `~/.venvs/weather` (for `mcp-srv-python` and
`FoundryConsoleV*python`), and all npm/NuGet dependencies. It is unrelated to
the snapshot state described below.

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
| MCP Server on Function App | `mcp-srv-func-app/mcp` | `func start` from `mcp-srv-func-app/mcp` (or VS Code **WeatherMcpSrvFuncApp**) | 8120 |
| MCP Server on Python | `mcp-srv-python/mcp` | `pip install -e "./mcp[dev]"` then `weather-mcp-srv-python` from `mcp-srv-python/mcp/`, with `MCP_SRV_PYTHON_KEY` set (see `mcp-srv-python/mcp/.env.example`) | 8140 |
| MCP Server on Node | `mcp-srv-node` | `npm ci` then `npm start` from `mcp-srv-node/`, with `MCP_SRV_NODE_KEY` set (see `mcp-srv-node/mcp/.env.example`) | 8150 |

### Non-obvious caveats

- Start `WeatherAPI` (8080) FIRST. Both the React UI (Vite proxies `/Home` and
 `/AIWeather` to `http://localhost:8080`, override with `VITE_API_DOTNET_URL`) and
 the Blazor UI (`API_DOTNET_URL` in `ui-blazor/blazor/appsettings.json`)
 depend on it. Without the API, React shows "Unable to load hello message" and
 Blazor's hello call fails.
- `WeatherMVC` is standalone (duplicates backend logic via `Core`/CQMediator) and
  does not call the API.
- Chat1b, Chat2b, Chat4b/Chat5b, and the V4 AI weather path need
  `MCP_SRV_PYTHON_URL`/`MCP_SRV_PYTHON_KEY` (Geo sub-agent in Chat4b/5b) and
  `MCP_SRV_NODE_URL`/`MCP_SRV_NODE_KEY` (NonAI Weather sub-agent) on api/mvc/worker, in
  addition to the existing `MCP_SRV_APP_SERVICE_*` (User sub-agent in Chat4b/5b) and
  `MCP_SRV_FUNC_APP_*` pairs.
- `mcp-srv-node` serves `GetPublicWeatherCurrent`/`GetPublicWeatherForecast`/`GetPublicWeatherHistory`;
  `mcp-srv-python` serves `GetLatLong`/`GetLocation` (Open-Meteo geocoding, Nominatim reverse
  geocoding); `mcp-srv-func-app` serves `GetCities` (largest cities near a coordinate, via Core's
  `GetCitiesHandler`); `mcp-srv-app-service` serves the saved-city tools
  `GetUser`/`AddUserCity`/`DeleteUserCity`. The geo and saved-city tools also exist in-process in Core
  (`Geo/Handlers`, `Users/Handlers`) for the local-loop paths.
  Never register the same tool name on two MCP hosts.
- `mcp-srv-app-service` needs `DB_CONNECTION_STRING` for its saved-city tools; without it the host
  still starts but `/About` reports unhealthy and tool calls fail.
- `GetCities` (in-process on api/mvc and remote on `mcp-srv-func-app`) queries `dbo.Cities`, a
  GeoNames `cities500` table loaded by the worker's daily `import-cities` Hangfire job (11:00 UTC,
  `batch-single`). `mcp-srv-func-app` therefore needs `DB_CONNECTION_STRING` too (without it
  `/about` reports unhealthy and GetCities fails), and GetCities returns no cities until
  `import-cities` has run once against that DB — trigger it from the worker's `/hangfire`.
- React's `BackendWakeGate` pings `mcp-srv-python`'s and `mcp-srv-node`'s `/Wake` at
  `http://localhost:8140/Wake` and `http://localhost:8150/Wake` like every other backend
  layer — without those servers running locally, `npm start` sits on the wake screen
  indefinitely (same as the existing MCP App Service/Func App targets).
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
  `.github/workflows/build-test.yml` builds each `.csproj` in Release +
  `npm ci && npm run build && npm test -- --run` in `ui-react`).
- React: `npm run build`, and `npm test -- --run` (Vitest).
- Shortcut for all of the below: `scripts/verify.sh --all` (or no args for just what changed).
- .NET test projects: `cqmediator-dotnet/cqmediator.tests`, `core-dotnet/core.tests`, `api-dotnet/api.tests`,
  `mvc-dotnet/mvc.tests`, `worker-dotnet/worker.tests`, `ui-blazor/blazor.tests`,
  `mcp-srv-app-service/mcp.tests`, and `mcp-srv-func-app/mcp.tests` (see CI
  `build-test.yml`).
- `mcp-srv-python` tests: `pip install -e "./mcp[dev]"` then `python -m pytest mcp.tests`
  from `mcp-srv-python/` (pytest, not a .NET test project).
- `FoundryConsoleV2python`/`V3python`/`V4python` tests: `pip install -e ".[dev]"` then
  `python -m pytest -q` from each folder (pytest; Python ports of the C# V2–V4 consoles).
- `mcp-srv-node` tests: `npm ci`, `npm run typecheck`, then `npm test -- --run` from
  `mcp-srv-node/` (Vitest). It runs TypeScript directly via Node 24 type stripping — no build step.
- On push to `main`, `build-test-provision-deploy.yml` calls `build-test.yml`,
  then `prod-provision-infra.yml` (`needs: [build_test]`), then every
  `prod-deploy-*.yml` in parallel (`needs: [provision]`). Each stage's
  workflow file can also be run standalone via `workflow_dispatch` on any
  branch (e.g. hotfixes).
- Production hosting is **Azure Container Apps + ACR** (five ASP.NET images
  plus one Python image for `mcp-srv-python` and one Node image for `mcp-srv-node`)
  plus **Functions on ACA** for
  `mcp-srv-func-app` and **Static Web Apps** for React.
  First-deploy bootstrap: `docs/aca-bootstrap.md`.
