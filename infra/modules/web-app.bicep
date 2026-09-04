// Reusable Linux Web App module, invoked once per app (5 times) from
// main.bicep. Each app gets its own dedicated User-Assigned Managed
// Identity (never SystemAssigned). No WEBSITES_PORT setting -- these apps
// are deployed via `dotnet publish` + zip-deploy onto the built-in Linux
// .NET runtime stack (not a custom container), which defaults to port
// 8080 and configures the container to match automatically. The per-app
// ports in launchSettings.json (8080/8090/8100/8110/8130) are a local
// `dotnet run`/debugging artifact only and have no bearing on the
// deployed container's port.

@description('Name of the Web App, e.g. wx1116-prod-api.')
param name string

param location string

@description('Resource ID of the shared Linux App Service Plan.')
param appServicePlanId string

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityClientId string

@description('Whether to set AZURE_CLIENT_ID to this app\'s UAMI client ID, so DefaultAzureCredential picks it unambiguously. Set true for api/mvc/worker.')
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
  {
    name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
    value: 'false'
  }
]

var clientIdAppSetting = [
  {
    name: 'AZURE_CLIENT_ID'
    value: userAssignedIdentityClientId
  }
]

var appSettings = setAzureClientId ? concat(baseAppSettings, clientIdAppSetting) : baseAppSettings

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
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
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      http20Enabled: true
      appSettings: appSettings
    }
  }
}

output id string = webApp.id
output name string = webApp.name
output defaultHostname string = webApp.properties.defaultHostName
