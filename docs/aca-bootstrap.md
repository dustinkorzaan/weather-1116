# ACA + ACR greenfield bootstrap

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
| Azure Container Registry | `wx1116prodacr` |
| Container Apps Environment | `wx1116-prod-aca-env` |
| Container Apps (ASP.NET) | `wx1116-prod-api`, `-mvc`, `-blazor`, `-worker`, `-mcp-srv-app-service` |
| Functions on ACA | `wx1116-prod-mcp-srv-func-app` |
| Static Web App | `wx1116-prod-react` |
| SQL Server + database | `wx1116-prod-sql-srv` / `wx1116-prod-sql-database` |
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
| `AZURE_SQL_DB_CONNECTION_STRING` | Hangfire + SQL for api/mvc/worker -- server/database only, no `Authentication` clause and no username/password (see below) |
| `AZURE_FOUNDRY_PROD_EUS2_KEY` | Foundry API key |
| `PROD_MCP_SRV_APP_SERVICE_KEY` | Bearer token for MCP app-service host |
| `PROD_MCP_SRV_FUNC_APP_KEY` | `mcp_extension` system key — you choose the value; deploy applies it |
| `GOOGLE_MAPS_API_KEY` | Maps on React/MVC/Blazor |
| `AZURE_UI_REACT_TOKEN` | SWA deploy token (after provision) |

## Step 1 — Provision infrastructure

Merge to `main` or run `provision-wx1116-prod-infra` via `workflow_dispatch`.
Provisioning and app deploys each trigger independently on push to `main` (no
`workflow_run` chain between them) and both run unconditionally on every
push. That is intentional, not wasteful — see
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

Unlike App Service `az webapp config appsettings set`, `az containerapp update
--set-env-vars` **replaces** the entire env-var list. Deploy workflows merge
deploy-time values onto Bicep-provisioned vars (App Insights, UAMI storage
settings, etc.) via `.github/scripts/aca-container-configure.sh`. Do not call
`--set-env-vars` directly in workflows without merging first.

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
