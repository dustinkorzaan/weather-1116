// Resource-group-scoped App Service deployment into the pre-existing
// wx1116-prod-rg resource group. wx1116-prod-github-actions-mi is created
// manually, not by this template; every other resource here is created (or
// updated in place) by `azd provision` and may already exist from a prior
// run.

targetScope = 'resourceGroup'

@description('azd environment name.')
param environmentName string = 'prod'

@description('Azure region for every resource in this environment except SQL, which uses the separate `sqlLocation` param below -- both currently default to the same region.')
param location string = 'centralus'

@description('Azure region for the SQL server/database only. Separate from `location` because East US 2 and East US have both (at least intermittently) rejected new Azure SQL server creation with RegionDoesNotAllowProvisioning; Central US does not have that restriction.')
param sqlLocation string = 'centralus'

@description('Short name prefix used to build resource names.')
param namePrefix string = 'wx1116'

@description('Name of the pre-existing resource group. Created manually, not by this template.')
param resourceGroupName string = 'wx1116-prod-rg'

@description('Name of the pre-existing GitHub Actions managed identity. Created manually, not by this template.')
param githubActionsIdentityName string = 'wx1116-prod-github-actions-mi'

@description('Name of the storage account backing the Function App\'s AzureWebJobsStorage.')
param storageAccountName string = 'wx1116prodblob'

@secure()
@description('SQL admin login username. Supply via azd env set / --parameters at deploy time.')
param sqlAdministratorLogin string

@secure()
@description('Bearer token for the MCP Server on App Service tool host, registered as the MyMcpSrvAppService Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvAppServiceKey string

@secure()
@description('Function key for the MCP Server on Function App tool host, registered as the MyMcpSrvFuncApp Foundry RemoteTool connection. Supply via azd env set / --parameters at deploy time.')
param mcpSrvFuncAppKey string

@description('Custom domain hostname to bind to the Static Web App, e.g. wx.korzaan.com. Its CNAME must already point at the Static Web App default hostname before this deploys, or validation fails. Empty skips custom domain binding.')
param staticWebAppCustomDomain string = 'wx.korzaan.com'

// Per-app identity configuration. Index 5 is the Function App MCP host.
var appIdentityConfig = [
  { key: 'api', name: '${namePrefix}-${environmentName}-api-mi' }
  { key: 'mvc', name: '${namePrefix}-${environmentName}-mvc-mi' }
  { key: 'blazor', name: '${namePrefix}-${environmentName}-blazor-mi' }
  { key: 'worker', name: '${namePrefix}-${environmentName}-worker-mi' }
  { key: 'mcp-srv-app-service', name: '${namePrefix}-${environmentName}-mcp-srv-app-service-mi' }
  { key: 'mcp-srv-func-app', name: '${namePrefix}-${environmentName}-mcp-srv-func-app-mi' }
]

var appServiceConfig = [
  { key: 'api', setAzureClientId: true, clientAffinityEnabled: false }
  { key: 'mvc', setAzureClientId: true, clientAffinityEnabled: false }
  { key: 'blazor', setAzureClientId: false, clientAffinityEnabled: true }
  { key: 'worker', setAzureClientId: true, clientAffinityEnabled: false }
  { key: 'mcp-srv-app-service', setAzureClientId: false, clientAffinityEnabled: false }
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

module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'app-service-plan'
  params: {
    name: '${namePrefix}-${environmentName}-asp'
    location: location
  }
}

module appServices 'modules/app-service.bicep' = [for (cfg, i) in appServiceConfig: {
  name: 'app-service-${cfg.key}'
  params: {
    name: '${namePrefix}-${environmentName}-${cfg.key}'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    userAssignedIdentityId: appIdentities[i].outputs.id
    userAssignedIdentityClientId: appIdentities[i].outputs.clientId
    setAzureClientId: cfg.setAzureClientId
    clientAffinityEnabled: cfg.clientAffinityEnabled
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}]

module functionApp 'modules/function-app.bicep' = {
  name: 'function-app'
  params: {
    name: '${namePrefix}-${environmentName}-mcp-srv-func-app'
    location: location
    appServicePlanId: appServicePlan.outputs.id
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
    serverName: '${namePrefix}-${environmentName}-sql-srv'
    databaseName: '${namePrefix}-${environmentName}-sql-database'
    location: sqlLocation
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
    mcpSrvAppServiceUrl: 'https://${appServices[4].outputs.defaultHostname}/mcp'
    mcpSrvFuncAppUrl: 'https://${functionApp.outputs.defaultHostname}/runtime/webhooks/mcp'
    mcpSrvAppServiceKey: mcpSrvAppServiceKey
    mcpSrvFuncAppKey: mcpSrvFuncAppKey
  }
}

output AZURE_RESOURCE_GROUP string = resourceGroupName

output APP_SERVICE_PLAN_NAME string = appServicePlan.outputs.name

output API_HOSTNAME string = appServices[0].outputs.defaultHostname
output MVC_HOSTNAME string = appServices[1].outputs.defaultHostname
output BLAZOR_HOSTNAME string = appServices[2].outputs.defaultHostname
output WORKER_HOSTNAME string = appServices[3].outputs.defaultHostname
output MCP_SRV_APP_SERVICE_HOSTNAME string = appServices[4].outputs.defaultHostname
output MCP_SRV_FUNC_APP_HOSTNAME string = functionApp.outputs.defaultHostname

output SQL_SERVER_FQDN string = sql.outputs.serverFullyQualifiedDomainName
output SQL_DATABASE_NAME string = sql.outputs.databaseName
output STORAGE_ACCOUNT_NAME string = functionApp.outputs.storageAccountName
output APP_INSIGHTS_CONNECTION_STRING string = monitoring.outputs.appInsightsConnectionString

output STATIC_WEB_APP_NAME string = staticWebApp.outputs.name
output STATIC_WEB_APP_HOSTNAME string = staticWebApp.outputs.defaultHostname
output STATIC_WEB_APP_CUSTOM_DOMAIN string = staticWebAppCustomDomain

output AI_FOUNDRY_ACCOUNT_NAME string = aiFoundry.outputs.accountName
output AI_FOUNDRY_PROJECT_NAME string = aiFoundry.outputs.projectName
output AI_FOUNDRY_MODEL_DEPLOYMENT_NAME string = aiFoundry.outputs.modelDeploymentName
output AI_FOUNDRY_MCP_SRV_APP_SERVICE_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvAppServiceConnectionName
output AI_FOUNDRY_MCP_SRV_FUNC_APP_CONNECTION_NAME string = aiFoundry.outputs.mcpSrvFuncAppConnectionName

output GITHUB_ACTIONS_IDENTITY_CLIENT_ID string = githubActionsIdentity.outputs.clientId
output GITHUB_ACTIONS_IDENTITY_PRINCIPAL_ID string = githubActionsIdentity.outputs.principalId

output MI_API_CLIENT_ID string = appIdentities[0].outputs.clientId
output MI_MVC_CLIENT_ID string = appIdentities[1].outputs.clientId
output MI_BLAZOR_CLIENT_ID string = appIdentities[2].outputs.clientId
output MI_WORKER_CLIENT_ID string = appIdentities[3].outputs.clientId
output MI_MCP_SRV_APP_SERVICE_CLIENT_ID string = appIdentities[4].outputs.clientId
output MI_MCP_SRV_FUNC_APP_CLIENT_ID string = appIdentities[5].outputs.clientId
