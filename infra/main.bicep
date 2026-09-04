// Resource-group-scoped: deploys directly into the pre-existing
// wx1116-prod-rg resource group (never creates it -- azd is told about it
// via the AZURE_RESOURCE_GROUP environment value, set once by the
// provisioning workflow). The resource group and the GitHub Actions
// managed identity are both created manually, along with a one-time Owner
// grant on the resource group -- see modules/managed-identity.bicep for
// how that identity's durable least-privilege footprint (Contributor +
// User Access Administrator, scoped to this resource group only) gets
// codified here instead. Deliberately NOT subscription-scoped: nothing
// here needs subscription-level resources, and staying resource-group-
// scoped means the deployment itself only ever needs the RG-scoped
// Contributor role already granted -- no subscription-wide permissions.

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

@description('App Service Plan SKU for the shared Linux plan.')
param appServicePlanSkuName string = 'B2'

@description('Name of the storage account backing the Function App\'s AzureWebJobsStorage. Must be globally unique, <=24 chars, lowercase alphanumeric only.')
param storageAccountName string = 'wx1116prodblob'

@secure()
@description('SQL admin login username. Treated as sensitive -- supply via azd env set / --parameters at deploy time, never committed. A non-default, non-obvious username is itself part of the security posture here, not just the password.')
param sqlAdministratorLogin string

// Per-app identity + web app configuration, in a fixed order shared by the
// app-identity loop (indices 0-4 below) and the web-app loop -- index 5 is
// the Function App's identity, consumed separately.
var appIdentityConfig = [
  { key: 'api', name: '${namePrefix}-${environmentName}-api-mi' }
  { key: 'mvc', name: '${namePrefix}-${environmentName}-mvc-mi' }
  { key: 'blazor', name: '${namePrefix}-${environmentName}-blazor-mi' }
  { key: 'worker', name: '${namePrefix}-${environmentName}-worker-mi' }
  { key: 'mcp-srv-app-service', name: '${namePrefix}-${environmentName}-mcp-srv-app-service-mi' }
  { key: 'mcp-srv-func-app', name: '${namePrefix}-${environmentName}-mcp-srv-func-app-mi' }
]

var webAppsConfig = [
  { key: 'api', setAzureClientId: true }
  { key: 'mvc', setAzureClientId: true }
  { key: 'blazor', setAzureClientId: false }
  { key: 'worker', setAzureClientId: true }
  { key: 'mcp-srv-app-service', setAzureClientId: false }
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

module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan'
  params: {
    name: '${namePrefix}-${environmentName}-asp'
    location: location
    skuName: appServicePlanSkuName
  }
}

module webApps 'modules/web-app.bicep' = [for (cfg, i) in webAppsConfig: {
  name: 'web-app-${cfg.key}'
  params: {
    name: '${namePrefix}-${environmentName}-${cfg.key}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    userAssignedIdentityId: appIdentities[i].outputs.id
    userAssignedIdentityClientId: appIdentities[i].outputs.clientId
    setAzureClientId: cfg.setAzureClientId
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}]

module functionApp 'modules/function-app.bicep' = {
  name: 'function-app'
  params: {
    name: '${namePrefix}-${environmentName}-mcp-srv-func-app'
    storageAccountName: storageAccountName
    location: location
    appServicePlanId: appServicePlan.outputs.id
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

output API_HOSTNAME string = webApps[0].outputs.defaultHostname
output MVC_HOSTNAME string = webApps[1].outputs.defaultHostname
output BLAZOR_HOSTNAME string = webApps[2].outputs.defaultHostname
output WORKER_HOSTNAME string = webApps[3].outputs.defaultHostname
output MCP_SRV_APP_SERVICE_HOSTNAME string = webApps[4].outputs.defaultHostname
output MCP_SRV_FUNC_APP_HOSTNAME string = functionApp.outputs.defaultHostname

output SQL_SERVER_FQDN string = sql.outputs.serverFullyQualifiedDomainName
output SQL_DATABASE_NAME string = sql.outputs.databaseName
output STORAGE_ACCOUNT_NAME string = functionApp.outputs.storageAccountName
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
