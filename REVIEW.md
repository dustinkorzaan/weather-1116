# REVIEW.md

The review checklist for every change to this repo. It is used by the
`peer-reviewer` and `final-reviewer` agents, by `/code-review`, and by human
reviewers. Each item is a rule; breaking one is a **BLOCKING** finding unless
it's marked otherwise.

## Correctness and tests

- [ ] Every behaviour change has a test in that stack's test project (xUnit, Vitest or pytest) that fails without the change.
- [ ] No test was skipped, disabled, deleted or loosened to get green.
- [ ] `scripts/verify.sh --all` is green. Only `infra` (Bicep) and Core's `[SqlServerFact]` tests may be SKIPPED locally.
- [ ] Async code awaits its work, flows `CancellationToken`, and disposes `HttpClient` responses and streams. There is no `.Result` or `.Wait()`.
- [ ] Null, empty and failure paths from external calls (Open-Meteo, Nominatim, GeoNames, Foundry, MCP hosts) are handled with a useful error, not a crash.

## Cross-stack contracts ([`docs/architecture.md`](docs/architecture.md))

- [ ] **Feature parity:** a UI behaviour change lands in **all three** UIs: React (`ui-react`), Blazor (`ui-blazor`) and MVC (`mvc-dotnet`). This means the same routes, data and interactions. Styling is independent, and the UIs share no CSS or components.
- [ ] **Backend parity:** backend behaviour exposed by `WeatherAPI` is mirrored in `WeatherMVC`, which duplicates logic via Core/CQMediator and does not call the API.
- [ ] **Theme contract** (Light/Dark/System, `weather-theme` in localStorage) and **Responsive Design Contract** (~320px → desktop, one fluid layout) still hold for touched UI.
- [ ] **MCP tools:** no tool name is registered on two MCP hosts. The host ↔ tool map in `AGENTS.md` and `docs/architecture.md` is unchanged, or updated, along with every caller (api/mvc/worker agent wiring, Foundry toolbox payloads).
- [ ] Shared logic lives in `core-dotnet` handlers, not copied into app projects. Exception: the parity duplication above, which is intentional.

## Configuration, infra and ops

- [ ] Each new env var or setting appears everywhere it's needed:
  - `appsettings*.json` or `.env.example`
  - `infra/` (Bicep app settings)
  - the matching `prod-deploy-*.yml` / `.github/scripts/aca-container-configure.sh`
  - `AGENTS.md` or the project README
- [ ] Missing optional config degrades gracefully, as the Google Maps key and `DB_CONNECTION_STRING` do today. The app still starts, and `/About` reports unhealthy rather than crashing.
- [ ] Ports and `launchSettings.json` are unchanged, or updated along with the port table in `AGENTS.md` and React's `BackendWakeGate` targets.
- [ ] EF model changes include a migration. Changes to `dbo.Cities` stay compatible with the worker's `import-cities` job.

## Security

- [ ] No secrets, keys, tokens or connection strings are committed. Samples use obvious placeholders.
- [ ] MCP host keys and API keys are read from configuration only and never logged.
- [ ] No new unauthenticated endpoint exposes user data. Existing POC exceptions (such as `/hangfire`) are not expanded.

## Scope and docs

- [ ] The diff contains only what the spec asks for: no drive-by refactors, debug leftovers, or commented-out code.
- [ ] `AGENTS.md` / `README.md` / `docs/architecture.md` are updated when setup, ports, env vars, topology or the tool map change.
- [ ] *(SHOULD)* Names, comment density and idioms match the surrounding code.
