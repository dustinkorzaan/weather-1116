# ACA + ACR greenfield bootstrap

First-time deployment into an empty `wx1116-prod-rg`. Two resources must exist
**before** the first provision or deploy can run at all:

- `wx1116-prod-github-mi` (user-assigned managed identity)
- Its GitHub OIDC federated credential for `repo:<owner>/<repo>:environment:prod`

Without both, `azure/login` and `azd auth login` in the workflows cannot
authenticate. Bicep never creates either one — it only references the identity
and assigns Contributor + User Access Administrator on this resource group.

## What gets provisioned

`azd provision` (via `prod-provision-infra.yml`) creates:

| Resource | Name |
| --- | --- |
| Azure Container Registry | `wx1116prodacr` |
| Container Apps Environment | `wx1116-prod-aca-env` |
| Container Apps (ASP.NET) | `wx1116-prod-api`, `-mvc`, `-blazor`, `-worker`, `-mcp-srv-app-service` |
| Container App (Python) | `wx1116-prod-mcp-srv-python` |
| Container App (Node.js) | `wx1116-prod-mcp-srv-node` |
| Functions on ACA | `wx1116-prod-mcp-srv-func-app` |
| Storage (Functions host) | `wx1116prodblob` |
| Static Web App | `wx1116-prod-react` |
| SQL Server (Entra-only auth) + database | `wx1116-prod-sql-srv` / `wx1116-prod-sql-database` |
| App Insights + Log Analytics | `wx1116-prod-appinsights` / `wx1116-prod-log` |
| AI Foundry | `wx1116-prod-res` / `wx1116-prod-proj` |
| Seven runtime managed identities | `wx1116-prod-*-mi` |

Everything uses `location: centralus`.

Every container app runs at the smallest Consumption-plan size ACA allows --
0.25 vCPU / 0.5Gi memory (sizes are per app in `containerAppsConfig` in
`infra/main.bicep`). Every app scales to zero (`minReplicas: 0`, including
blazor and worker) with a 30-minute cooldown before scaling in, so an idle
environment costs nothing between requests without cold-starting on every
short gap in traffic. `worker` alone is capped at `maxReplicas: 1`: Hangfire
recurring jobs assume a single active server, so a second cold-started
replica racing the first would double-run jobs instead of adding throughput.

Two consequences of scaling everything to zero are worth knowing before you
rely on this in production, not just during the demo:

- **`worker`'s recurring jobs** (`RecurringJobScheduler`: the `Cron.Daily(2)` AI weather
  checks and the 11:00 UTC `import-cities` GeoNames load) only
  run if a replica happens to be up when Hangfire's scheduler ticks. There is
  no queue-depth/KEDA scale rule bringing `worker` up on a schedule -- the
  only thing that wakes it from zero is an inbound HTTP request, which today
  means the React UI's `useBackendWake` hook (`ui-react/src/app/useBackendWake.js`,
  used from `App.jsx`) pinging `worker`'s own `/About` directly on page load,
  in parallel with API, MVC, Blazor, and all four MCP hosts, rather than relying
  on API's `/About` fan-out to reach it (see below for why that changed).
  `useBackendWake` fires a fresh ping per target roughly every 30s -- without
  cancelling ones still in flight -- until each succeeds, rather than giving
  up after a single attempt; see the `BackendWakeScreen` full-page loader,
  now showing seven spinning icons (one per target), it drives while that's
  pending. If nobody loads the React UI around 2am,
  that day's recurring jobs are silently skipped, not just delayed. Keep `worker` at
  `minReplicas: 1` (or add a scheduled wake, e.g. a Logic App/cron hitting
  `/About`) if the recurring jobs need to actually run unattended.
  Once a job *is* running, `ProcessingJobKeepAlive` (worker) pings the
  replica's own `/Wake` through ingress (`CONTAINER_APP_HOSTNAME`) every 5
  minutes while Hangfire reports any job processing, so a long job such as
  `import-cities` is not killed by scale-in 30 minutes after the last request
  (which surfaces as `SqlException: Operation cancelled by user`).
- **`mcp-srv-func-app` cold starts used to compound with `AboutClient`'s 60s
  HTTP timeout** (`api-dotnet/api/Program.cs`). API's own `/About` still fans
  out server-side to worker and both MCP hosts via `Task.WhenAll`, but that
  fan-out only starts once the API container itself has finished cold
  starting -- so a page load that waited on `/About` alone paid API's cold
  start, then worker/MCP's cold start, back to back. `useBackendWake` now
  pings `worker` and `mcp-srv-app-service` directly at their own `/About`
  (matching `AboutController`'s casing) and `mcp-srv-func-app` at its own
  lowercase `/about` route, at the same time it pings API, so their cold
  start begins immediately instead of only after API wakes up and gets
  around to calling them -- turning that serial chain into one round of
  parallel cold starts. The three new targets' base URLs come from
  `VITE_MCP_SRV_APP_SERVICE_URL` / `VITE_MCP_SRV_FUNC_APP_URL` (new) and the
  existing `VITE_WORKER_DOTNET_URL`; the React deploy and CI build workflows
  set these from the same `PROD_MCP_SRV_APP_SERVICE_URL` /
  `PROD_MCP_SRV_FUNC_APP_URL` GitHub variables the API, MVC, and worker
  deploys already use, since Vite inlines `VITE_*` at build time.
  API's `/About` fan-out and its 60s timeout are unchanged and still matter
  for the About dialog's own request and for Foundry tool calls into
  `mcp-srv-func-app`; if those start timing out, raising this host back to
  `minReplicas: 1` (and/or its old 0.5 vCPU / 1Gi size) is still the first
  thing to try before touching the 60s timeout itself. `useBackendWake` never
  cancels an in-flight request when it fires the next retry, so a ping that
  takes close to the full cold-start window still counts as a success once it
  finally responds -- the retries just add redundant requests rather than
  racing the slow one to a premature abort.

## Prerequisites (GitHub)

**Secrets** (must exist before provision/deploy):

| Secret | Purpose |
| --- | --- |
| `AZURE_GITHUB_MI_CLIENTID` | GitHub Actions MI client ID |
| `AZURE_TENANTID` | Azure AD tenant |
| `AZURE_SUBSCRIPTIONID` | Target subscription |

**Secrets** (before app deploys):

| Secret | Purpose |
| --- | --- |
| `AZURE_SQL_DB_CONNECTION_STRING` | Hangfire + SQL for api/mvc/worker -- server/database only, no `Authentication` clause and no username/password (see below) |
| `PROD_MCP_SRV_APP_SERVICE_KEY` | Bearer token for MCP app-service host |
| `PROD_MCP_SRV_FUNC_APP_KEY` | `mcp_extension` system key — you choose the value; deploy applies it |
| `PROD_MCP_SRV_PYTHON_KEY` | Bearer token for the standalone Python MCP host |
| `PROD_MCP_SRV_NODE_KEY` | Bearer token for the standalone Node.js MCP host |
| `GOOGLE_MAPS_API_KEY` | Maps on React/MVC/Blazor |
| `AZURE_UI_REACT_TOKEN` | SWA deploy token (after provision) |
| `PROD_APPINSIGHTS_CONNECTION_STRING` | Browser telemetry for React -- same value as the `APP_INSIGHTS_CONNECTION_STRING` infra output. Missing/empty is safe (React just runs with no browser telemetry), but leaving it unset makes browser telemetry silently absent once the backends start reporting. |

## Step 1 — Provision infrastructure

Merge to `main`, or run `provision-wx1116-prod-infra` directly via
`workflow_dispatch`. On push to `main`, the orchestrator
`build-test-provision-deploy.yml` runs `build-test.yml` first and only calls
provision (`needs: [build_test]`) once that succeeds — provisioning runs
unconditionally on every such push (not just when `infra/**` changed), which
is intentional, not wasteful — see
[Provision vs. deploy ownership](#provision-vs-deploy-ownership) for what makes
re-provisioning safe.

Provisioning locally needs the same pre-pass the workflow runs:

```bash
az login   # the preprovision hook queries the resource group with `az`
azd env select prod
azd provision
```

Capture outputs from the provision job or:

```bash
azd env get-values
```

## Step 2 — Populate GitHub vars from provision outputs

Set `https://` URLs from the `*_HOSTNAME` outputs. For React, use the custom
domain when provision binds one (default `wx.korzaan.com` via
`STATIC_WEB_APP_CUSTOM_DOMAIN`); fall back to `STATIC_WEB_APP_HOSTNAME` only
when custom-domain binding is skipped.

```text
PROD_API_DOTNET_URL          = https://<API_HOSTNAME>
PROD_MVC_DOTNET_URL          = https://<MVC_HOSTNAME>
PROD_UI_BLAZOR_URL           = https://<BLAZOR_HOSTNAME>
PROD_WORKER_DOTNET_URL       = https://<WORKER_HOSTNAME>
PROD_MCP_SRV_APP_SERVICE_URL = https://<MCP_SRV_APP_SERVICE_HOSTNAME>
PROD_MCP_SRV_FUNC_APP_URL    = https://<MCP_SRV_FUNC_APP_HOSTNAME>
PROD_MCP_SRV_PYTHON_URL      = https://<MCP_SRV_PYTHON_HOSTNAME>
PROD_MCP_SRV_NODE_URL        = https://<MCP_SRV_NODE_HOSTNAME>
PROD_UI_REACT_URL            = https://<STATIC_WEB_APP_CUSTOM_DOMAIN>
```

Also set Foundry vars (`AZURE_FOUNDRY_PROD_PROJ_URL`,
`AZURE_FOUNDRY_ARM_ACCOUNT_NAME`, `AZURE_FOUNDRY_PROD_MODEL`,
`AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME`, `AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME`)
against `wx1116-prod-proj` / `wx1116-prod-res`. No `AZURE_FOUNDRY_PROD_KEY`
secret is needed for the app deploys — api/mvc/worker authenticate to Foundry
via their managed identity instead (see docs/architecture.md); that key is
only for the FoundryConsoleV1-V5 local dev-tool consoles, each with its own
`.env`. These are the only Foundry var/secret names this repo reads.

## Step 3 — Static Web App deploy token

```bash
az staticwebapp secrets list \
  --name wx1116-prod-react \
  --resource-group wx1116-prod-rg \
  --query properties.apiKey -o tsv
```

Store as GitHub secret `AZURE_UI_REACT_TOKEN`.

### `AZURE_SQL_DB_CONNECTION_STRING` format (Entra managed identity, no password)

api/mvc/worker authenticate to Azure SQL via their own user-assigned managed
identity, not a SQL login. The secret should hold only the server/database
portion -- **no** `Authentication`, `User ID`, or `Password` keyword:

```text
Server=tcp:wx1116-prod-sql-srv.database.windows.net,1433;Initial Catalog=wx1116-prod-sql-database;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30
```

Each app appends `Authentication=Active Directory Default` plus its own
`AZURE_CLIENT_ID` as `User ID` at startup
(`Core.Data.ManagedIdentitySqlConnectionStringFactory`), so
`DefaultAzureCredential` resolves that app's specific identity rather than
one of the other five provisioned in this environment. If the secret already
contains an `Authentication` clause, the factory leaves it untouched --
useful for a one-off SQL-login override during local testing, but not the
production shape.

## Step 4 — SQL contained users (once)

Run `infra/scripts/create-contained-users.sql` as the SQL Entra admin
(`wx1116-prod-github-mi`). See comments in
`prod-provision-infra.yml`. Re-run this if the runtime managed identities are
deleted and recreated (new principal IDs), or when an app gains SQL access --
`mcp-srv-func-app`'s identity is a read-only (`db_datareader`) user, since
GetCities only reads `dbo.Cities`.

After the first deploy, `dbo.Cities` is empty until the worker's daily
`import-cities` job runs (11:00 UTC); trigger it once from the worker's
`/hangfire` dashboard (Recurring Jobs → `import-cities` → Trigger now) so
GetCities has data straight away.

## Step 5 — Deploy apps

On push to `main`, `build-test-provision-deploy.yml` calls each deploy
workflow (`needs: [provision]`) once provisioning succeeds, running them all
in parallel. Each can also be run directly via `workflow_dispatch`:

| Workflow file | Target |
| --- | --- |
| `prod-deploy-api.yml` | Container App + ACR image |
| `prod-deploy-mvc.yml` | Container App + ACR image |
| `prod-deploy-blazor.yml` | Container App + ACR image |
| `prod-deploy-worker.yml` | Container App + ACR image |
| `prod-deploy-mcp-srv-app.yml` | Container App + ACR image |
| `prod-deploy-mcp-srv-func.yml` | Functions-on-ACA container image (ACR) |
| `prod-deploy-mcp-srv-python.yml` | Container App + ACR image |
| `prod-deploy-mcp-srv-node.yml` | Container App + ACR image |
| `prod-deploy-react.yml` | Static Web App |
| `prod-deploy-foundry-agents.yml` | Foundry agents (`wx1116-agent-for-current-weather`, `wx1116-agent-for-chat`) |

Deploys build a Docker image remotely with `az acr build`, push it to ACR,
and roll it out with `az containerapp update` (composite action
`.github/actions/deploy-aca-container`), which also merges deploy-time
environment variables and secrets onto the Bicep-provisioned baseline.

### MCP `mcp_extension` key

`PROD_MCP_SRV_FUNC_APP_KEY` is the source of truth for the Functions
`x-functions-key`, not something read back out of Azure. Generate a value and
store it as that GitHub secret **before** the first func deploy:

```bash
openssl rand -base64 32
```

`prod-deploy-mcp-srv-func.yml` then applies it to the Functions host's
`mcp_extension` system key on every deploy, and api/mvc/worker read the same
secret into their own container app secrets. Because both sides come from one
GitHub secret, there is no copy-back step and no redeploy ordering requirement.

To rotate: update the GitHub secret, then re-run the func deploy plus the
api/mvc/worker deploys.

If the secret is unset, the func deploy fails with that instruction rather than
generating a key nothing else knows about.

### Container App environment variables

`az containerapp update --set-env-vars` **replaces** the entire env-var list.
Deploy workflows merge deploy-time values onto Bicep-provisioned vars (App
Insights, UAMI storage settings, etc.) via
`.github/scripts/aca-container-configure.sh`. Do not call `--set-env-vars`
directly in workflows without merging first.

### Provision vs. deploy ownership

Two pipelines write to the same container apps, so each field has exactly one
owner:

| Field | Owner |
| --- | --- |
| App existence, ingress, scale, identity, ACR registry | `infra/main.bicep` (provision) |
| Functions host settings, `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID` | `infra/main.bicep` (provision) |
| Container image | `prod-deploy-*.yml` (deploy) |
| All other env vars, and every secret | `prod-deploy-*.yml` (deploy) |

The catch is that an ARM/Bicep deployment is a **PUT**, not a PATCH: any
property the template sets wins over whatever was configured out of band. Left
alone, every provision would reset all six apps to the placeholder image with
only the provision-owned env vars and no secrets — an outage on every push to
`main`, with the deploy workflows racing to repair it.

So provision reads the deploy-owned fields back and hands them through:

1. The `preprovision` hook in `azure.yaml` runs
   `infra/scripts/capture-existing-container-apps.sh`, which lists the resource
   group and sets `EXISTING_CONTAINER_APP_KEYS` (e.g. `api,mvc,worker`) in the
   azd environment. It needs an authenticated `az`, and fails the provision
   rather than reporting apps as absent if it cannot list them.
2. `infra/modules/existing-container-app.bicep` resolves each listed app as an
   `existing` reference and returns its live image, env vars, and secrets.
3. `container-app.bicep` / `functions-container-app.bicep` reuse the live image,
   pass the live secrets straight through, and union the env lists —
   provision-owned names are reasserted (so a rotated App Insights connection
   string still lands) while every other name is carried forward.

Consequences worth knowing:

- The placeholder image applies on **first create only**. It is
  `mcr.microsoft.com/dotnet/samples:aspnetapp`, a runnable ASP.NET app that
  serves on port 8080 like the real images, so the first revision goes healthy;
  a bare runtime image such as `dotnet/aspnet:10.0` has no app to run and
  crash-loops instead.
- Adding a provision-owned env var means adding it to the module's
  `provisionEnvVars`. Adding a deploy-time var means adding it to the workflow's
  `env_overlay_multiline`. Putting the same name in both makes provision win.
- Deleting a container app by hand is fine: the next capture pass simply omits
  it and provision recreates it from the placeholder.

The Functions deploy workflow builds a .NET 10 isolated-worker container image
(`mcp-srv-func-app/mcp/Dockerfile`) and pushes it to ACR. Functions-on-ACA
requires container images; zip/package deploy via `Azure/functions-action` targets
App Service (`Microsoft.Web/sites`), not `Microsoft.App/containerApps`.

`aca-functions-mcp-key.sh` checks that `az containerapp function keys` exists
before setting the `mcp_extension` system key; if the CLI command group is
missing, the job fails with manual-setup guidance instead of silently skipping
MCP auth. That command group connects live to the running Functions host
inside the newest revision rather than going through ARM, so the script polls
`az containerapp revision show` (up to 5 minutes) until the just-deployed
revision is `Running`/`Healthy` before touching keys -- `az containerapp
update` in the previous step returns as soon as the update is accepted, not
once the new revision is actually healthy, and without this wait the key
list/set call intermittently fails with a generic `Error setting function key`
and no further detail.

## Step 6 — Foundry MCP tools and agents

Do not configure MCP servers or agents in the Foundry portal.

- **Tools (connections):** `infra/modules/ai-foundry.bicep` registers
  `MyMcpSrvAppService` and `MyMcpSrvFuncApp` as `RemoteTool` + `CustomKeys`
  connections (URL + auth header) on every `azd provision`.
- **Toolbox + agents:** `prod-deploy-foundry-agents.yml` publishes
  `wx1116-geo-nonaiweather-toolbox` (wrapping those connections), then publishes
  `wx1116-agent-for-current-weather` and `wx1116-agent-for-chat` with the
  toolbox attached (`require_approval: never`).

| Connection | URL | Auth |
| --- | --- | --- |
| MyMcpSrvFuncApp | `https://<func-host>/runtime/webhooks/mcp` | Header `x-functions-key` |
| MyMcpSrvAppService | `https://<mcp-app-host>/mcp` | Bearer token |

## Step 7 — Validate

- Each app: `GET https://<host>/About`
- API `/About` aggregates worker + all four MCP hosts
- React SWA: hello, map, `/current-ai-weather`, `/chat-clients`
- React custom domain (when bound): `https://<STATIC_WEB_APP_CUSTOM_DOMAIN>`
  returns `200` with a valid TLS certificate
- MCP Inspector: see `docs/6-mcp-inspection/6-mcp-inspection.md`
- A container app that has scaled to zero takes a few seconds to answer its
  first request after an idle gap longer than the 30-minute cooldown -- that
  is expected, not a failure.

## Static Web App custom domain

`infra/main.bicep` binds a custom domain to `wx1116-prod-react` via
`infra/modules/static-web-app.bicep`. The default hostname is
`wx.korzaan.com` (`staticWebAppCustomDomain` / `STATIC_WEB_APP_CUSTOM_DOMAIN`).

**Before the first provision that binds a custom domain**, create a CNAME at your
DNS provider pointing the hostname at the SWA default hostname
(`STATIC_WEB_APP_HOSTNAME`, e.g.
`wonderful-ground-0511f4e0f.5.azurestaticapps.net`). Bicep uses
`cname-delegation` validation — Azure checks that CNAME during deploy; if it is
missing or wrong, provision fails.

```text
wx.korzaan.com  CNAME  <STATIC_WEB_APP_HOSTNAME>
```

No manual `az staticwebapp hostname` step is required once DNS is in place.
Provision creates the `Microsoft.Web/staticSites/customDomains` resource and
Azure issues the managed certificate after validation succeeds.

Override or skip via azd env:

```bash
# different hostname
azd env set STATIC_WEB_APP_CUSTOM_DOMAIN other.example.com

# skip custom-domain binding (e.g. a throwaway SWA in another environment)
azd env set STATIC_WEB_APP_CUSTOM_DOMAIN ""
```

ACA container-app custom domains remain out of scope for this template; only the
React Static Web App is wired here.
