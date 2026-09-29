// Resource-group-scoped ACA + ACR deployment into the pre-existing
// wx1116-prod-rg resource group. wx1116-prod-github-mi is created
// manually, not by this template; every other resource here is created (or
// updated in place) by `azd provision` and may already exist from a prior
// run.

targetScope = 'resourceGroup'

@description('azd environment name.')
param environmentName string = 'prod'

@description('Azure region for every resource in this environment.')
param location string = 'centralus'

@description('Short name prefix used to build resource names.')
param namePrefix string = 'wx1116'

@description('Name of the pre-existing resource group. Created manually, not by this template.')
param resourceGroupName string = 'wx1116-prod-rg'

@description('Name of the pre-existing GitHub Actions managed identity. Created manually, not by this template.')
param githubActionsIdentityName string = 'wx1116-prod-github-mi'

@description('Globally unique ACR name (alphanumeric only).')
param acrName string = 'wx1116prodacr'

@description('Name of the storage account backing the Functions-on-ACA AzureWebJobsStorage.')
param storageAccountName string = 'wx1116prodblob'

@description('Comma-separated keys of the container apps that already exist, e.g. api,mvc,worker. Set by infra/scripts/capture-existing-container-apps.sh before azd provision; empty means first provision.')
param existingContainerAppKeys string = ''

@secure()
@description('Bearer token for the MCP Server on App Service tool host, registered as the MyMcpSrvAppService Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvAppServiceKey string

@secure()
@description('Function key for the MCP Server on Function App tool host, registered as the MyMcpSrvFuncApp Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvFuncAppKey string

@secure()
@description('Bearer token for the standalone Python MCP server tool host, registered as the MyMcpSrvPython Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvPythonKey string

@secure()
@description('Bearer token for the standalone Node.js MCP server tool host, registered as the MyMcpSrvNode Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvNodeKey string

@description('Custom domain hostname to bind to the Static Web App, e.g. wx.korzaan.com. Its CNAME must already point at the Static Web App default hostname before this deploys, or validation fails. Empty skips custom domain binding.')
param staticWebAppCustomDomain string = 'wx.korzaan.com'

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var acrPushRoleId = '8311e382-0749-4cb8-b61a-304f252e45ec'

// First create only. dotnet/samples:aspnetapp is a runnable ASP.NET app that
// serves on 8080, the same port the real images use, so the first revision goes
// healthy. A bare runtime image such as dotnet/aspnet:10.0 has no entrypoint app
// and crash-loops instead.
var placeholderImage = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

var existingKeys = empty(existingContainerAppKeys) ? [] : split(existingContainerAppKeys, ',')

// Per-app identity configuration. Indices 0-6 align 1:1 with containerAppsConfig
// below (generic container apps). Index 7 is the Functions-on-ACA MCP host, which
// isn't in containerAppsConfig and is referenced by its own hardcoded index instead.
var appIdentityConfig = [
  { key: 'api', name: '${namePrefix}-${environmentName}-api-mi' }
  { key: 'mvc', name: '${namePrefix}-${environmentName}-mvc-mi' }
  { key: 'blazor', name: '${namePrefix}-${environmentName}-blazor-mi' }
  { key: 'worker', name: '${namePrefix}-${environmentName}-worker-mi' }
  { key: 'mcp-srv-app-service', name: '${namePrefix}-${environmentName}-mcp-srv-app-service-mi' }
  { key: 'mcp-srv-python', name: '${namePrefix}-${environmentName}-mcp-srv-python-mi' }
  { key: 'mcp-srv-node', name: '${namePrefix}-${environmentName}-mcp-srv-node-mi' }
  { key: 'mcp-srv-func-app', name: '${namePrefix}-${environmentName}-mcp-srv-func-app-mi' }
]

// Every app scales to zero (minReplicas 0) on the same extended cooldown --
// see modules/container-app.bicep's cooldownPeriod default. worker stays
// capped at maxReplicas 1: Hangfire recurring jobs assume a single active
// server, so a second cold-started replica racing the first would double-run
// jobs instead of adding throughput.
var containerAppsConfig = [
  { key: 'api', setAzureClientId: true, maxReplicas: 5, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
  { key: 'mvc', setAzureClientId: true, maxReplicas: 5, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
  { key: 'blazor', setAzureClientId: false, maxReplicas: 5, stickySessions: true, cpu: '0.25', memory: '0.5Gi' }
  { key: 'worker', setAzureClientId: true, maxReplicas: 1, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
  { key: 'mcp-srv-app-service', setAzureClientId: true, maxReplicas: 5, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
  { key: 'mcp-srv-python', setAzureClientId: false, maxReplicas: 5, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
  { key: 'mcp-srv-node', setAzureClientId: false, maxReplicas: 5, stickySessions: false, cpu: '0.25', memory: '0.5Gi' }
]

module githubActionsIdentity 'modules/managed-identity.bicep' = {
  name: 'github-actions-identity'
  params: {
    githubActionsIdentityName: githubActionsIdentityName
  }
}

module appIdentities 'modules/app-identity.bicep' = [for cfg in appIdentityConfig: {
  name: 'app-identity-${cfg.key}'
  params: {
    name: cfg.name
    location: location
  }
}]

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    logAnalyticsName: '${namePrefix}-${environmentName}-log'
    appInsightsName: '${namePrefix}-${environmentName}-appinsights'
    location: location
  }
}

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    name: acrName
    location: location
  }
}

module acaEnvironment 'modules/aca-environment.bicep' = {
  name: 'aca-environment'
  params: {
    name: '${namePrefix}-${environmentName}-aca-env'
    location: location
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

module acrPushForGitHubActions 'modules/acr-role-assignment.bicep' = {
  name: 'acr-push-github-actions'
  params: {
    registryName: acr.outputs.name
    principalId: githubActionsIdentity.outputs.principalId
    roleDefinitionId: acrPushRoleId
    assignmentName: guid(acr.outputs.id, githubActionsIdentity.outputs.principalId, acrPushRoleId)
  }
}

module acrPullForApps 'modules/acr-role-assignment.bicep' = [for (cfg, i) in containerAppsConfig: {
  name: 'acr-pull-${cfg.key}'
  params: {
    registryName: acr.outputs.name
    principalId: appIdentities[i].outputs.principalId
    roleDefinitionId: acrPullRoleId
    assignmentName: guid(acr.outputs.id, appIdentities[i].outputs.principalId, acrPullRoleId)
  }
}]

module acrPullForFunctionsApp 'modules/acr-role-assignment.bicep' = {
  name: 'acr-pull-mcp-srv-func-app'
  params: {
    registryName: acr.outputs.name
    principalId: appIdentities[7].outputs.principalId
    roleDefinitionId: acrPullRoleId
    assignmentName: guid(acr.outputs.id, appIdentities[7].outputs.principalId, acrPullRoleId)
  }
}

// Provision owns whether these apps exist and their ingress/scale/identity
// wiring; prod-deploy-*.yml owns their image, deploy-time env vars, and secrets.
// Since a Bicep deployment is a PUT, the deploy-owned half has to be read back
// and handed straight through, or every provision would revert the apps to the
// placeholder image with no app settings.
module existingContainerApps 'modules/existing-container-app.bicep' = [for cfg in containerAppsConfig: {
  name: 'existing-container-app-${cfg.key}'
  params: {
    name: '${namePrefix}-${environmentName}-${cfg.key}'
    exists: contains(existingKeys, cfg.key)
  }
}]

module existingFunctionsContainerApp 'modules/existing-container-app.bicep' = {
  name: 'existing-container-app-mcp-srv-func-app'
  params: {
    name: '${namePrefix}-${environmentName}-mcp-srv-func-app'
    exists: contains(existingKeys, 'mcp-srv-func-app')
  }
}

@batchSize(1)
module containerApps 'modules/container-app.bicep' = [for (cfg, i) in containerAppsConfig: {
  name: 'container-app-${cfg.key}'
  params: {
    name: '${namePrefix}-${environmentName}-${cfg.key}'
    location: location
    managedEnvironmentId: acaEnvironment.outputs.id
    containerImage: placeholderImage
    existingImage: existingContainerApps[i].outputs.image
    existingEnv: existingContainerApps[i].outputs.env
    existingSecrets: existingContainerApps[i].outputs.secrets
    maxReplicas: cfg.maxReplicas
    stickySessions: cfg.stickySessions
    cpu: cfg.cpu
    memory: cfg.memory
    userAssignedIdentityId: appIdentities[i].outputs.id
    userAssignedIdentityClientId: appIdentities[i].outputs.clientId
    setAzureClientId: cfg.setAzureClientId
    acrLoginServer: acr.outputs.loginServer
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}]

module functionsContainerApp 'modules/functions-container-app.bicep' = {
  name: 'functions-container-app'
  params: {
    name: '${namePrefix}-${environmentName}-mcp-srv-func-app'
    location: location
    managedEnvironmentId: acaEnvironment.outputs.id
    storageAccountName: storageAccountName
    blobDataContributorPrincipalIds: [
      appIdentities[3].outputs.principalId // worker: import-cities batches in the temp container
    ]
    userAssignedIdentityId: appIdentities[7].outputs.id
    userAssignedIdentityPrincipalId: appIdentities[7].outputs.principalId
    userAssignedIdentityClientId: appIdentities[7].outputs.clientId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    acrLoginServer: acr.outputs.loginServer
    existingImage: existingFunctionsContainerApp.outputs.image
    existingEnv: existingFunctionsContainerApp.outputs.env
    existingSecrets: existingFunctionsContainerApp.outputs.secrets
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    serverName: '${namePrefix}-${environmentName}-sql-srv'
    databaseName: '${namePrefix}-${environmentName}-sql-database'
    location: location
    entraAdminPrincipalId: githubActionsIdentity.outputs.principalId
    entraAdminLoginName: githubActionsIdentityName
  }
}

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'static-web-app'
  params: {
    name: '${namePrefix}-${environmentName}-react'
    location: location
    customDomainName: staticWebAppCustomDomain
  }
}

module aiFoundry 'modules/ai-foundry.bicep' = {
  name: 'ai-foundry'
  params: {
    accountName: '${namePrefix}-${environmentName}-res'
    projectName: '${namePrefix}-${environmentName}-proj'
    location: location
    customSubDomainName: toLower('${namePrefix}${environmentName}${uniqueString(subscription().id, resourceGroupName)}')
    appInsightsId: monitoring.outputs.appInsightsId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    grantedPrincipalIds: [
      appIdentities[0].outputs.principalId // api
      appIdentities[1].outputs.principalId // mvc
      appIdentities[3].outputs.principalId // worker
    ]
    githubActionsPrincipalId: githubActionsIdentity.outputs.principalId
    mcpSrvAppServiceUrl: 'https://${containerApps[4].outputs.fqdn}/mcp'
    mcpSrvFuncAppUrl: 'https://${functionsContainerApp.outputs.fqdn}/runtime/webhooks/mcp'
    mcpSrvPythonUrl: 'https://${containerApps[5].outputs.fqdn}/mcp'
    mcpSrvNodeUrl: 'https://${containerApps[6].outputs.fqdn}/mcp'
    mcpSrvAppServiceKey: mcpSrvAppServiceKey
    mcpSrvFuncAppKey: mcpSrvFuncAppKey
    mcpSrvPythonKey: mcpSrvPythonKey
    mcpSrvNodeKey: mcpSrvNodeKey
  }
}

output AZURE_RESOURCE_GROUP string = resourceGroupName

output ACR_LOGIN_SERVER string = acr.outputs.loginServer
output ACR_NAME string = acr.outputs.name
output ACA_ENVIRONMENT_NAME string = acaEnvironment.outputs.name
output ACA_DEFAULT_DOMAIN string = acaEnvironment.outputs.defaultDomain

output API_HOSTNAME string = containerApps[0].outputs.fqdn
output MVC_HOSTNAME string = containerApps[1].outputs.fqdn
output BLAZOR_HOSTNAME string = containerApps[2].outputs.fqdn
output WORKER_HOSTNAME string = containerApps[3].outputs.fqdn
output MCP_SRV_APP_SERVICE_HOSTNAME string = containerApps[4].outputs.fqdn
output MCP_SRV_PYTHON_HOSTNAME string = containerApps[5].outputs.fqdn
output MCP_SRV_NODE_HOSTNAME string = containerApps[6].outputs.fqdn
output MCP_SRV_FUNC_APP_HOSTNAME string = functionsContainerApp.outputs.fqdn

output SQL_SERVER_FQDN string = sql.outputs.serverFullyQualifiedDomainName
output SQL_DATABASE_NAME string = sql.outputs.databaseName
output STORAGE_ACCOUNT_NAME string = functionsContainerApp.outputs.storageAccountName
output APP_INSIGHTS_CONNECTION_STRING string = monitoring.outputs.appInsightsConnectionString

output STATIC_WEB_APP_NAME string = staticWebApp.outputs.name
output STATIC_WEB_APP_HOSTNAME string = staticWebApp.outputs.defaultHostname
output STATIC_WEB_APP_CUSTOM_DOMAIN string = staticWebAppCustomDomain

output AI_FOUNDRY_ACCOUNT_NAME string = aiFoundry.outputs.accountName
output AI_FOUNDRY_PROJECT_NAME string = aiFoundry.outputs.projectName
output AI_FOUNDRY_MODEL_DEPLOYMENT_NAME string = aiFoundry.outputs.modelDeploymentName
output AI_FOUNDRY_MCP_SRV_APP_SERVICE_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvAppServiceConnectionName
output AI_FOUNDRY_MCP_SRV_FUNC_APP_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvFuncAppConnectionName
output AI_FOUNDRY_MCP_SRV_PYTHON_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvPythonConnectionName
output AI_FOUNDRY_MCP_SRV_NODE_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvNodeConnectionName

output GITHUB_ACTIONS_IDENTITY_CLIENT_ID string = githubActionsIdentity.outputs.clientId
output GITHUB_ACTIONS_IDENTITY_PRINCIPAL_ID string = githubActionsIdentity.outputs.principalId

output MI_API_CLIENT_ID string = appIdentities[0].outputs.clientId
output MI_MVC_CLIENT_ID string = appIdentities[1].outputs.clientId
output MI_BLAZOR_CLIENT_ID string = appIdentities[2].outputs.clientId
output MI_WORKER_CLIENT_ID string = appIdentities[3].outputs.clientId
output MI_MCP_SRV_APP_SERVICE_CLIENT_ID string = appIdentities[4].outputs.clientId
output MI_MCP_SRV_PYTHON_CLIENT_ID string = appIdentities[5].outputs.clientId
output MI_MCP_SRV_NODE_CLIENT_ID string = appIdentities[6].outputs.clientId
output MI_MCP_SRV_FUNC_APP_CLIENT_ID string = appIdentities[7].outputs.clientId
