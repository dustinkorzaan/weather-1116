// Functions-on-ACA host for mcp-srv-func-app (MCP extension + x-functions-key auth).
// Code deploys via GitHub Actions (dotnet publish + Azure/functions-action), not ACR images.

@description('Container app name, e.g. wx1116-prod-mcp-srv-func-app.')
param name string

param location string

@description('Resource ID of the Container Apps Environment.')
param managedEnvironmentId string

@description('Storage account name backing AzureWebJobsStorage.')
param storageAccountName string

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Principal ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityPrincipalId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityClientId string

param appInsightsConnectionString string

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

// Placeholder until the first Functions package deploy lands.
var placeholderImage = 'mcr.microsoft.com/azure-functions/dotnet-isolated4:4-dotnet-isolated9.0'

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

resource functionContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: managedEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
      }
      secrets: []
    }
    template: {
      containers: [
        {
          name: name
          image: placeholderImage
          env: [
            {
              name: 'FUNCTIONS_EXTENSION_VERSION'
              value: '~4'
            }
            {
              name: 'FUNCTIONS_WORKER_RUNTIME'
              value: 'dotnet-isolated'
            }
            {
              name: 'AzureWebJobsSecretStorageType'
              value: 'blob'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
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
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
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

output id string = functionContainerApp.id
output name string = functionContainerApp.name
output fqdn string = functionContainerApp.properties.configuration.ingress.fqdn
output storageAccountName string = storageAccount.name
