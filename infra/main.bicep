// Resource-group-scoped greenfield ACA + ACR deployment into the pre-existing
// wx1116-prod-rg resource group. Only wx1116-prod-github-actions-mi exists
// before first provision; everything else is created here.

targetScope = 'resourceGroup'

@description('azd environment name.')
param environmentName string = 'prod'

@description('Azure region for every resource in this environment.')
param location string = 'eastus2'

@description('Short name prefix used to build resource names.')
param namePrefix string = 'wx1116'

@description('Name of the pre-existing resource group. Created manually, not by this template.')
param resourceGroupName string = 'wx1116-prod-rg'

@description('Name of the pre-existing GitHub Actions managed identity. Created manually, not by this template.')
param githubActionsIdentityName string = 'wx1116-prod-github-actions-mi'

@description('GitHub repository in owner/repo form, used for the federated credential subject.')
param githubRepository string = 'dustinkorzaan/weather-1116'

@description('Globally unique ACR name (alphanumeric only).')
param acrName string = 'wx1116prodacr'

@description('Name of the storage account backing the Functions-on-ACA AzureWebJobsStorage.')
param storageAccountName string = 'wx1116prodblob'

@secure()
@description('SQL admin login username. Supply via azd env set / --parameters at deploy time.')
param sqlAdministratorLogin string

var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var acrPushRoleId = '8311e349-0899-44f8-b5e2-9be9d40fb000'
var placeholderImage = 'mcr.microsoft.com/dotnet/aspnet:10.0'

// Per-app identity configuration. Index 5 is the Functions-on-ACA MCP host.
var appIdentityConfig = [
  { key: 'api', name: '${namePrefix}-${environmentName}-api-mi' }
  { key: 'mvc', name: '${namePrefix}-${environmentName}-mvc-mi' }
  { key: 'blazor', name: '${namePrefix}-${environmentName}-blazor-mi' }
  { key: 'worker', name: '${namePrefix}-${environmentName}-worker-mi' }
  { key: 'mcp-srv-app-service', name: '${namePrefix}-${environmentName}-mcp-srv-app-service-mi' }
  { key: 'mcp-srv-func-app', name: '${namePrefix}-${environmentName}-mcp-srv-func-app-mi' }
]

var containerAppsConfig = [
  { key: 'api', setAzureClientId: true, minReplicas: 0, maxReplicas: 3, stickySessions: false }
  { key: 'mvc', setAzureClientId: true, minReplicas: 0, maxReplicas: 3, stickySessions: false }
  { key: 'blazor', setAzureClientId: false, minReplicas: 1, maxReplicas: 3, stickySessions: true }
  { key: 'worker', setAzureClientId: true, minReplicas: 1, maxReplicas: 1, stickySessions: false }
  { key: 'mcp-srv-app-service', setAzureClientId: false, minReplicas: 0, maxReplicas: 2, stickySessions: false }
]

module githubActionsIdentity 'modules/managed-identity.bicep' = {
  name: 'github-actions-identity'
  params: {
    githubActionsIdentityName: githubActionsIdentityName
    githubRepository: githubRepository
    githubEnvironmentName: environmentName
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
    logAnalyticsName: '${namePrefix}-${environmentName}-eastus2-log'
    appInsightsName: '${namePrefix}-${environmentName}-eastus2-appinsights'
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

module containerApps 'modules/container-app.bicep' = [for (cfg, i) in containerAppsConfig: {
  name: 'container-app-${cfg.key}'
  params: {
    name: '${namePrefix}-${environmentName}-${cfg.key}'
    location: location
    managedEnvironmentId: acaEnvironment.outputs.id
    containerImage: placeholderImage
    minReplicas: cfg.minReplicas
    maxReplicas: cfg.maxReplicas
    stickySessions: cfg.stickySessions
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
    userAssignedIdentityId: appIdentities[5].outputs.id
    userAssignedIdentityPrincipalId: appIdentities[5].outputs.principalId
    userAssignedIdentityClientId: appIdentities[5].outputs.clientId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    serverName: '${namePrefix}-${environmentName}-sql-server'
    databaseName: '${namePrefix}-${environmentName}-sql-database'
    location: location
    administratorLogin: sqlAdministratorLogin
    entraAdminPrincipalId: githubActionsIdentity.outputs.principalId
    entraAdminLoginName: githubActionsIdentityName
  }
}

module staticWebApp 'modules/static-web-app.bicep' = {
  name: 'static-web-app'
  params: {
    name: '${namePrefix}-${environmentName}-react'
    location: location
  }
}

module aiFoundry 'modules/ai-foundry.bicep' = {
  name: 'ai-foundry'
  params: {
    accountName: '${namePrefix}-${environmentName}-eastus2-res'
    projectName: '${namePrefix}-${environmentName}-eastus2-prj'
    location: location
    customSubDomainName: toLower('${namePrefix}${environmentName}eastus2${uniqueString(subscription().id, resourceGroupName)}')
    appInsightsId: monitoring.outputs.appInsightsId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    grantedPrincipalIds: [
      appIdentities[0].outputs.principalId // api
      appIdentities[1].outputs.principalId // mvc
      appIdentities[3].outputs.principalId // worker
    ]
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
output MCP_SRV_FUNC_APP_HOSTNAME string = functionsContainerApp.outputs.fqdn

output SQL_SERVER_FQDN string = sql.outputs.serverFullyQualifiedDomainName
output SQL_DATABASE_NAME string = sql.outputs.databaseName
output STORAGE_ACCOUNT_NAME string = functionsContainerApp.outputs.storageAccountName
output APP_INSIGHTS_CONNECTION_STRING string = monitoring.outputs.appInsightsConnectionString

output STATIC_WEB_APP_NAME string = staticWebApp.outputs.name
output STATIC_WEB_APP_HOSTNAME string = staticWebApp.outputs.defaultHostname

output AI_FOUNDRY_ACCOUNT_NAME string = aiFoundry.outputs.accountName
output AI_FOUNDRY_PROJECT_NAME string = aiFoundry.outputs.projectName

output GITHUB_ACTIONS_IDENTITY_CLIENT_ID string = githubActionsIdentity.outputs.clientId
output GITHUB_ACTIONS_IDENTITY_PRINCIPAL_ID string = githubActionsIdentity.outputs.principalId

output MI_API_CLIENT_ID string = appIdentities[0].outputs.clientId
output MI_MVC_CLIENT_ID string = appIdentities[1].outputs.clientId
output MI_BLAZOR_CLIENT_ID string = appIdentities[2].outputs.clientId
output MI_WORKER_CLIENT_ID string = appIdentities[3].outputs.clientId
output MI_MCP_SRV_APP_SERVICE_CLIENT_ID string = appIdentities[4].outputs.clientId
output MI_MCP_SRV_FUNC_APP_CLIENT_ID string = appIdentities[5].outputs.clientId
