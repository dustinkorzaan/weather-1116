// Storage account (required for AzureWebJobsStorage regardless of hosting
// model) + the Function App itself, Dedicated -- hosted on the same shared
// Linux App Service Plan as the 5 web apps, not a separate Flex Consumption
// plan. Uses its own dedicated User-Assigned Managed Identity for
// identity-based storage access -- no storage key anywhere.

@description('Name of the Function App, e.g. wx1116-prod-mcp-srv-func-app.')
param name string

@description('Name of the storage account backing AzureWebJobsStorage, e.g. wx1116prodblob. Storage account names must be globally unique, <=24 chars, lowercase alphanumeric only.')
param storageAccountName string

param location string

@description('Resource ID of the shared Linux App Service Plan.')
param appServicePlanId string

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Principal ID of this app\'s dedicated User-Assigned Managed Identity, for the storage role assignments.')
param userAssignedIdentityPrincipalId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity, required by the identity-based AzureWebJobsStorage connection since this is user-assigned rather than system-assigned.')
param userAssignedIdentityClientId string

param appInsightsConnectionString string

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  kind: 'functionapp,linux'
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
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
      http20Enabled: true
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'true'
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccount.name
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'AzureWebJobsStorage__clientId'
          value: userAssignedIdentityClientId
        }
      ]
    }
  }
}

resource storageBlobDataOwnerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, userAssignedIdentityPrincipalId, storageBlobDataOwnerRoleId)
  scope: storageAccount
  properties: {
    principalId: userAssignedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataOwnerRoleId)
  }
}

resource storageQueueDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, userAssignedIdentityPrincipalId, storageQueueDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: userAssignedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
  }
}

resource storageTableDataContributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, userAssignedIdentityPrincipalId, storageTableDataContributorRoleId)
  scope: storageAccount
  properties: {
    principalId: userAssignedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
  }
}

output id string = functionApp.id
output name string = functionApp.name
output defaultHostname string = functionApp.properties.defaultHostName
output storageAccountName string = storageAccount.name
