// Log Analytics workspace + workspace-based Application Insights. Invoked
// twice from main.bicep: once for the primary (West US 2) region, once for
// East US 2 (paired with the AI Foundry resources, so tracing doesn't have
// to reach cross-region into the West US 2 workspace).

@description('Name of the Log Analytics workspace to create.')
param logAnalyticsName string

@description('Name of the Application Insights instance to create.')
param appInsightsName string

param location string

@description('Log retention in days.')
param retentionInDays int = 30

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output appInsightsId string = appInsights.id
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
