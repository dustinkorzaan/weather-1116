# App Service greenfield bootstrap

First-time deployment into an empty `wx1116-prod-rg`. Two resources must exist
**before** the first provision or deploy can run at all:

- `wx1116-prod-github-actions-mi` (user-assigned managed identity)
- Its GitHub OIDC federated credential for `repo:<owner>/<repo>:environment:prod`

Without both, `azure/login` and `azd auth login` in the workflows cannot
authenticate. Bicep never creates either one — it only references the identity
and assigns Contributor + User Access Administrator on this resource group.

## What gets provisioned

`azd provision` (via `prod-provision-infra.yml`) creates:

| Resource | Name |
| --- | --- |
| Linux App Service Plan (B1) | `wx1116-prod-asp` |
| App Services (ASP.NET) | `wx1116-prod-api`, `-mvc`, `-blazor`, `-worker`, `-mcp-srv-app-service` |
| Function App (Linux, dedicated plan) | `wx1116-prod-mcp-srv-func-app` |
| Storage (Functions host) | `wx1116prodblob` |
| Static Web App | `wx1116-prod-react` |
| SQL Server + database | `wx1116-prod-sql-srv` / `wx1116-prod-sql-database` |
| App Insights + Log Analytics | `wx1116-prod-appinsights` / `wx1116-prod-log` |
| AI Foundry | `wx1116-prod-res` / `wx1116-prod-proj` |
| Six runtime managed identities | `wx1116-prod-*-mi` |

Everything uses `location: centralus` except SQL, which already defaults to
Central US (`sqlLocation`).

## Prerequisites (GitHub)

**Secrets** (must exist before provision/deploy):

| Secret | Purpose |
| --- | --- |
| `AZURE_GITHUB_CLIENTID` | GitHub Actions MI client ID |
| `AZURE_TENANTID` | Azure AD tenant |
| `AZURE_SUBSCRIPTIONID` | Target subscription |
| `WX1116_SQL_ADMIN_LOGIN_NAME` | SQL native admin username for Bicep |

**Secrets** (before app deploys):

| Secret | Purpose |
| --- | --- |
| `AZURE_SQL_DB_CONNECTION_STRING` | Hangfire + SQL for api/mvc/worker -- server/database only, no `Authentication` clause and no username/password (see below) |
| `AZURE_FOUNDRY_PROD_KEY` | Foundry API key |
| `PROD_MCP_SRV_APP_SERVICE_KEY` | Bearer token for MCP app-service host |
| `PROD_MCP_SRV_FUNC_APP_KEY` | `mcp_extension` system key — you choose the value; deploy applies it |
| `GOOGLE_MAPS_API_KEY` | Maps on React/MVC/Blazor |
| `AZURE_UI_REACT_TOKEN` | SWA deploy token (after provision) |

## Step 1 — Provision infrastructure

Merge to `main`, or run `provision-wx1116-prod-infra` directly via
`workflow_dispatch`. On push to `main`, the orchestrator
`build-test-provision-deploy.yml` runs `build-test.yml` first and only calls
provision (`needs: [build_test]`) once that succeeds — provisioning runs
unconditionally on every such push (not just when `infra/**` changed), which
is intentional, not wasteful — see
[Provision vs. deploy ownership](#provision-vs-deploy-ownership).

Provisioning locally:

```bash
az login
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
when custom-domain binding is skipped. App Service hostnames are
`<app>.azurewebsites.net`.

```text
PROD_API_DOTNET_URL          = https://<API_HOSTNAME>
PROD_MVC_DOTNET_URL          = https://<MVC_HOSTNAME>
PROD_UI_BLAZOR_URL           = https://<BLAZOR_HOSTNAME>
PROD_WORKER_DOTNET_URL       = https://<WORKER_HOSTNAME>
PROD_MCP_SRV_APP_SERVICE_URL = https://<MCP_SRV_APP_SERVICE_HOSTNAME>
PROD_MCP_SRV_FUNC_APP_URL    = https://<MCP_SRV_FUNC_APP_HOSTNAME>
PROD_UI_REACT_URL            = https://<STATIC_WEB_APP_CUSTOM_DOMAIN>
```

Also set Foundry vars (`AZURE_FOUNDRY_PROD_PROJ_URL`,
`AZURE_FOUNDRY_PROD_MODEL`, `AZURE_FOUNDRY_PROD_CURRENT_WX_AGENT_NAME`,
`AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME`, plus the `AZURE_FOUNDRY_PROD_KEY` secret
on the app deploys) against `wx1116-prod-proj`. These are the only Foundry
var/secret names this repo reads -- create them fresh under these names if
this is your first bootstrap; if you're migrating an older checkout, the
`_EUS2_` and `_CUS_` variants are retired and nothing falls back to them.

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
(`wx1116-prod-github-actions-mi`). See comments in
`prod-provision-infra.yml`. Re-run this if the runtime managed identities are
deleted and recreated (new principal IDs).

## Step 5 — Deploy apps

On push to `main`, `build-test-provision-deploy.yml` calls each deploy
workflow (`needs: [provision]`) once provisioning succeeds, running them all
in parallel. Each can also be run directly via `workflow_dispatch`:

| Workflow file | Target |
| --- | --- |
| `prod-deploy-api.yml` | App Service zip deploy |
| `prod-deploy-mvc.yml` | App Service zip deploy |
| `prod-deploy-blazor.yml` | App Service zip deploy |
| `prod-deploy-worker.yml` | App Service zip deploy |
| `prod-deploy-mcp-srv-app.yml` | App Service zip deploy |
| `prod-deploy-mcp-srv-func.yml` | Function App zip deploy |
| `prod-deploy-react.yml` | Static Web App |
| `prod-deploy-foundry-agents.yml` | Foundry agents (`wx1116-agent-for-current-weather`, `wx1116-agent-for-chat`) |

Deploys use `dotnet publish` + `az webapp/functionapp deploy` (composite
action `.github/actions/deploy-app-service`), then upsert application
settings with `az webapp/functionapp config appsettings set`.

### MCP `mcp_extension` key

`PROD_MCP_SRV_FUNC_APP_KEY` is the source of truth for the Functions
`x-functions-key`, not something read back out of Azure. Generate a value and
store it as that GitHub secret **before** the first func deploy:

```bash
openssl rand -base64 32
```

`prod-deploy-mcp-srv-func.yml` then applies it to the Functions host's
`mcp_extension` system key on every deploy, and api/mvc/worker read the same
secret into their own app settings. Because both sides come from one GitHub
secret, there is no copy-back step and no redeploy ordering requirement.

`.github/scripts/function-app-mcp-key.sh` retries `az functionapp keys
list/set` until the Functions host answers. Zip-deploy returns when ARM
succeeds, not when the host is serving, so ARM `state=Running` is not a
readiness signal.

To rotate: update the GitHub secret, then re-run the func deploy plus the
api/mvc/worker deploys.

If the secret is unset, the func deploy fails with that instruction rather than
generating a key nothing else knows about.

### Provision vs. deploy ownership

| Field | Owner |
| --- | --- |
| App Service Plan, sites, Function App, identity, SQL, SWA, Foundry | `infra/main.bicep` (provision) |
| Functions host settings, `ASPNETCORE_ENVIRONMENT`, `APPLICATIONINSIGHTS_CONNECTION_STRING`, `AZURE_CLIENT_ID` | `infra/main.bicep` (provision) |
| App code (zip) | `prod-deploy-*.yml` (deploy) |
| All other application settings | `prod-deploy-*.yml` (deploy) via `az webapp/functionapp config appsettings set` (upsert) |

A Bicep deployment is a **PUT**. The site modules declare a short
`siteConfig.appSettings` list, so re-provision resets application settings to
that list until the following deploy upserts the rest. On push to `main` the
orchestrator always deploys after provision (`needs: [provision]`). Running
`prod-provision-infra` alone leaves settings at the reduced set until the next
deploy.

## Step 6 — Foundry MCP tools and agents

Do not configure MCP servers or agents in the Foundry portal.

- **Tools (connections):** `infra/modules/ai-foundry.bicep` registers
  `MyMcpSrvAppService` and `MyMcpSrvFuncApp` as `RemoteTool` + `CustomKeys`
  connections (URL + auth header) on every `azd provision`.
- **Agents:** `prod-deploy-foundry-agents.yml` publishes
  `wx1116-agent-for-current-weather` and `wx1116-agent-for-chat` against those
  connections with `require_approval: never`.

| Connection | URL | Auth |
| --- | --- | --- |
| MyMcpSrvFuncApp | `https://<func-host>/runtime/webhooks/mcp` | Header `x-functions-key` |
| MyMcpSrvAppService | `https://<mcp-app-host>/mcp` | Bearer token |

## Step 7 — Validate

- Each app: `GET https://<host>/About`
- API `/About` aggregates worker + both MCP hosts
- React SWA: hello, map, `/current-ai-weather`, `/chat-clients`
- React custom domain (when bound): `https://<STATIC_WEB_APP_CUSTOM_DOMAIN>`
  returns `200` with a valid TLS certificate
- MCP Inspector: see `docs/6-mcp-inspection/6-mcp-inspection.md`

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

App Service custom domains are out of scope for this template; only the React
Static Web App is wired here.
