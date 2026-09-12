// Reusable Linux App Service (Web App) module for ASP.NET workloads. Code
// deploys separately via `az webapp deploy` (prod-deploy-*.yml); this module
// owns the site resource, its identity, and the application settings listed
// here. `siteConfig.appSettings` is a full PUT of the whole settings
// collection: every `azd provision` resets it to exactly this list, dropping
// whatever `az webapp config appsettings set` added afterward
// (DB_CONNECTION_STRING, Foundry/MCP/Maps keys, CORS origins, inter-app
// URLs) until the next deploy upserts them again -- see the ordering note in
// prod-provision-infra.yml. A provision-only run with no following deploy
// leaves settings at this reduced list.

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

@description('Enable client affinity (ARR session-affinity cookie). true for Blazor Server (SignalR needs sticky sessions); false for everything else so a plan scale-out does not skew load.')
param clientAffinityEnabled bool = false

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
  {
    name: 'WEBSITE_RUN_FROM_PACKAGE'
    value: '1'
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
    clientAffinityEnabled: clientAffinityEnabled
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: appSettings
    }
  }
}

output id string = site.id
output name string = site.name
output defaultHostname string = site.properties.defaultHostName
