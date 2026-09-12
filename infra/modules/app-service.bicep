// Reusable Linux App Service (Web App) module for ASP.NET workloads. Code
// deploys separately via `az webapp deploy` (prod-deploy-*.yml); this module
// only owns the site resource, its identity, and a small set of
// provision-owned application settings (App Insights, UAMI client ID). Any
// other application settings the deploy workflow sets via
// `az webapp config appsettings set` are left alone by this module and, since
// that command only upserts the keys it's given, survive future redeploys of
// this template untouched.

@description('App Service name, e.g. wx1116-prod-api.')
param name string

param location string

@description('Resource ID of the shared App Service Plan.')
param appServicePlanId string

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityClientId string

@description('Whether to set AZURE_CLIENT_ID so DefaultAzureCredential picks this UAMI.')
param setAzureClientId bool = false

param appInsightsConnectionString string

var baseAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
]

var clientIdAppSetting = [
  {
    name: 'AZURE_CLIENT_ID'
    value: userAssignedIdentityClientId
  }
]

var appSettings = setAzureClientId ? concat(baseAppSettings, clientIdAppSetting) : baseAppSettings

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      appSettings: appSettings
    }
  }
}

output id string = site.id
output name string = site.name
output defaultHostname string = site.properties.defaultHostName
