# ACA + ACR greenfield bootstrap

First-time deployment into an empty `wx1116-prod-rg` (only
`wx1116-prod-github-actions-mi` exists beforehand).

## What gets provisioned

`azd provision` (via `prod-provision-infra.yml`) creates:

| Resource | Name |
| --- | --- |
| Azure Container Registry | `wx1116prodacr` |
| Container Apps Environment | `wx1116-prod-aca-env` |
| Container Apps (ASP.NET) | `wx1116-prod-api`, `-mvc`, `-blazor`, `-worker`, `-mcp-srv-app-service` |
| Functions on ACA | `wx1116-prod-mcp-srv-func-app` |
| Static Web App | `wx1116-prod-react` |
| SQL Server + database | `wx1116-prod-sql-server` / `wx1116-prod-sql-database` |
| App Insights + Log Analytics | `wx1116-prod-eastus2-appinsights` |
| AI Foundry | `wx1116-prod-eastus2-res` / `-prj` |
| Six runtime managed identities | `wx1116-prod-*-mi` |

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
| `AZURE_SQL_DB_CONNECTION_STRING` | Hangfire + SQL for api/mvc/worker |
| `AZURE_FOUNDRY_PROD_EUS2_KEY` | Foundry API key |
| `PROD_MCP_SRV_APP_SERVICE_KEY` | Bearer token for MCP app-service host |
| `PROD_MCP_SRV_FUNC_APP_KEY` | `mcp_extension` system key (set after first func deploy) |
| `GOOGLE_MAPS_API_KEY` | Maps on React/MVC/Blazor |
| `AZURE_UI_REACT_TOKEN` | SWA deploy token (after provision) |

## Step 1 — Provision infrastructure

Merge to `main` or run `provision-wx1116-prod-infra` via `workflow_dispatch`.
Provisioning and app deploys each trigger independently on push to `main` (no
`workflow_run` chain between them) and both run unconditionally on every
push — `azd provision` is idempotent, so this is intentional, not wasteful.

Capture outputs from the provision job or:

```bash
azd env select prod
azd env get-values
```

## Step 2 — Populate GitHub vars from provision outputs

Set `https://` URLs from the `*_HOSTNAME` outputs:

```text
PROD_API_DOTNET_URL          = https://<API_HOSTNAME>
PROD_MVC_DOTNET_URL          = https://<MVC_HOSTNAME>
PROD_UI_BLAZOR_URL           = https://<BLAZOR_HOSTNAME>
PROD_WORKER_DOTNET_URL       = https://<WORKER_HOSTNAME>
PROD_MCP_SRV_APP_SERVICE_URL = https://<MCP_SRV_APP_SERVICE_HOSTNAME>
PROD_MCP_SRV_FUNC_APP_URL    = https://<MCP_SRV_FUNC_APP_HOSTNAME>
PROD_UI_REACT_URL            = https://<STATIC_WEB_APP_HOSTNAME>
```

Also set Foundry vars (`AZURE_FOUNDRY_PROD_EUS2_PROJ_URL`, model, agent names).

## Step 3 — Static Web App deploy token

```bash
az staticwebapp secrets list \
  --name wx1116-prod-react \
  --resource-group wx1116-prod-rg \
  --query properties.apiKey -o tsv
```

Store as GitHub secret `AZURE_UI_REACT_TOKEN`.

## Step 4 — SQL contained users (once)

Run `infra/scripts/create-contained-users.sql` as the SQL Entra admin
(`wx1116-prod-github-actions-mi`). See comments in
`prod-provision-infra.yml`.

## Step 5 — Deploy apps

Deploy workflows trigger automatically on push to `main` (independently of
provision), or run them directly via `workflow_dispatch`:

| Workflow file | Target |
| --- | --- |
| `prod-deploy-api.yml` | Container App + ACR image |
| `prod-deploy-mvc.yml` | Container App + ACR image |
| `prod-deploy-blazor.yml` | Container App + ACR image |
| `prod-deploy-worker.yml` | Container App + ACR image |
| `prod-deploy-mcp-srv-app.yml` | Container App + ACR image |
| `prod-deploy-mcp-srv-func.yml` | Functions-on-ACA container image (ACR) |
| `prod-deploy-react.yml` | Static Web App |

After the MCP func deploy, retrieve the `mcp_extension` key if needed:

```bash
az containerapp function keys list \
  --name wx1116-prod-mcp-srv-func-app \
  --resource-group wx1116-prod-rg \
  --key-type systemKey \
  --query "keys[?name=='mcp_extension'].value | [0]" -o tsv
```

Update `PROD_MCP_SRV_FUNC_APP_KEY` and redeploy api/mvc/worker.

### Container App environment variables

Unlike App Service `az webapp config appsettings set`, `az containerapp update
--set-env-vars` **replaces** the entire env-var list. Deploy workflows merge
deploy-time values onto Bicep-provisioned vars (App Insights, UAMI storage
settings, etc.) via `.github/scripts/aca-container-configure.sh`. Do not call
`--set-env-vars` directly in workflows without merging first.

The Functions deploy workflow builds a .NET 10 isolated-worker container image
(`mcp-srv-func-app/mcp/Dockerfile`) and pushes it to ACR. Functions-on-ACA
requires container images; zip/package deploy via `Azure/functions-action` targets
App Service (`Microsoft.Web/sites`), not `Microsoft.App/containerApps`.

`aca-functions-mcp-key.sh` checks that `az containerapp function keys` exists
before creating the `mcp_extension` system key; if the CLI command group is
missing, the job fails with manual-setup guidance instead of silently skipping
MCP auth.

## Step 6 — Foundry MCP servers

In the Foundry portal, configure MCP servers for agents:

| Server | URL | Auth |
| --- | --- | --- |
| McpSrvFuncApp | `https://<func-host>/runtime/webhooks/mcp` | Header `x-functions-key` |
| McpSrvAppService | `https://<mcp-app-host>/mcp` | Bearer token |

Set `require_approval: never` on each tool.

## Step 7 — Validate

- Each app: `GET https://<host>/About`
- API `/About` aggregates worker + both MCP hosts
- React SWA: hello, map, `/current-ai-weather`, `/chat-clients`
- MCP Inspector: see `docs/6-mcp-inspection/6-mcp-inspection.md`

## Custom domains

Out of scope for the initial ACA deploy. Map domains to ACA ingress and SWA
in a follow-up story.
