// AI Foundry resource + project (new unified Foundry model, not the older
// ML-workspace-based Hub). Connects to the East US 2 App Insights instance
// for tracing, grants api/mvc/worker's managed identities passwordless
// Cognitive Services User access, deploys the gpt-5.4-mini model on the
// account, and registers the two MCP tool hosts as Custom Keys connections
// on the project (MyMcpSrvAppService, MyMcpSrvFuncApp) so hosted agents can
// reference them as tools.
//
// NOTE: the Microsoft.CognitiveServices API surface for the unified
// Foundry resource/project/connections/deployments model has moved around
// across preview versions. Verify the api-version and resource shapes below
// against current docs before relying on this module.

@description('Name of the AI Foundry resource, e.g. wx1116-prod-eastus2-res.')
param accountName string

@description('Name of the AI Foundry project, e.g. wx1116-prod-eastus2-prj.')
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

@description('Base URL of the MCP Server on App Service tool host, e.g. https://wx1116-prod-mcp-srv-app-service.<domain>/mcp.')
param mcpSrvAppServiceUrl string

@description('Base URL of the MCP Server on Function App tool host, e.g. https://wx1116-prod-mcp-srv-func-app.<domain>/runtime/webhooks/mcp.')
param mcpSrvFuncAppUrl string

@secure()
@description('Bearer token for the MCP Server on App Service tool host (sent as the Authorization header value, including the "Bearer " prefix).')
param mcpSrvAppServiceKey string

@secure()
@description('Function key for the MCP Server on Function App tool host (sent as the x-functions-key header value).')
param mcpSrvFuncAppKey string

@description('Model deployment name on the Foundry account, e.g. gpt-5.4-mini.')
param modelDeploymentName string = 'gpt-5.4-mini'

@description('Underlying model name to deploy, e.g. gpt-5.4-mini.')
param modelName string = 'gpt-5.4-mini'

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

// Default settings: GlobalStandard SKU, capacity 1, no explicit model
// version pin (Azure OpenAI resolves to the current default version for the
// model when version is omitted).
resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-04-01-preview' = {
  parent: foundryAccount
  name: modelDeploymentName
  sku: {
    name: 'GlobalStandard'
    capacity: 1
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
    }
  }
}

// Custom Keys connections register the two MCP tool hosts on the project so
// hosted agents (wx1116-agent-current-weather, wx1116-agent-chat) can reference them
// as MCP tools by connection name instead of embedding raw secrets in each
// agent definition. Each connection stores the tool's base URL as `target`
// and its single auth header as one entry under `credentials.keys`.
resource mcpSrvAppServiceConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-04-01-preview' = {
  parent: foundryProject
  name: 'MyMcpSrvAppService'
  properties: {
    category: 'CustomKeys'
    target: mcpSrvAppServiceUrl
    authType: 'CustomKeys'
    isSharedToAll: true
    credentials: {
      keys: {
        Authorization: 'Bearer ${mcpSrvAppServiceKey}'
      }
    }
  }
}

resource mcpSrvFuncAppConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-04-01-preview' = {
  parent: foundryProject
  name: 'MyMcpSrvFuncApp'
  properties: {
    category: 'CustomKeys'
    target: mcpSrvFuncAppUrl
    authType: 'CustomKeys'
    isSharedToAll: true
    credentials: {
      keys: {
        'x-functions-key': mcpSrvFuncAppKey
      }
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
output mcpSrvAppServiceConnectionName string = mcpSrvAppServiceConnection.name
output mcpSrvFuncAppConnectionName string = mcpSrvFuncAppConnection.name
output modelDeploymentName string = modelDeployment.name
