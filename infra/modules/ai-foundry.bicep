// AI Foundry resource + project (new unified Foundry model, not the older
// ML-workspace-based Hub), provisioned empty -- no model deployment; that's
// a manual follow-up once this exists. Connects to the East US 2 App
// Insights pair for tracing, and grants api/mvc/worker's managed identities
// passwordless Cognitive Services User access. This does not touch the
// existing prod Foundry resource or its AZURE_FOUNDRY_PROD_EUS2_KEY-based
// access in any way.
//
// NOTE: the Microsoft.CognitiveServices API surface for the unified
// Foundry resource/project/connections model has moved around across
// preview versions. Verify the api-version and the connections resource
// shape below against current docs before relying on this module.

@description('Name of the AI Foundry resource, e.g. wx1116-prd-eastus2-res.')
param accountName string

@description('Name of the AI Foundry project, e.g. wx1116-prd-eastus2-prj.')
param projectName string

param location string

@description('Globally-unique custom subdomain for the Foundry resource\'s public endpoint.')
param customSubDomainName string

@description('Resource ID of the East US 2 Application Insights instance to connect for tracing.')
param appInsightsId string

@description('Connection string of the East US 2 Application Insights instance.')
param appInsightsConnectionString string

@description('Principal IDs of the api/mvc/worker managed identities to grant Cognitive Services User on this Foundry resource.')
param grantedPrincipalIds array

var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' = {
  name: accountName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: customSubDomainName
    allowProjectManagement: true
    publicNetworkAccess: 'Enabled'
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview' = {
  parent: foundryAccount
  name: projectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

resource appInsightsConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-04-01-preview' = {
  parent: foundryProject
  name: 'appinsights-eastus2'
  properties: {
    category: 'AppInsights'
    target: appInsightsId
    authType: 'ApiKey'
    isSharedToAll: true
    credentials: {
      key: appInsightsConnectionString
    }
  }
}

resource cognitiveServicesUserAssignments 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for principalId in grantedPrincipalIds: {
  name: guid(foundryAccount.id, principalId, cognitiveServicesUserRoleId)
  scope: foundryAccount
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
  }
}]

output accountId string = foundryAccount.id
output accountName string = foundryAccount.name
output projectName string = foundryProject.name
