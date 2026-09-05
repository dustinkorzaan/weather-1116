// Functions-on-ACA host for mcp-srv-func-app (MCP extension + x-functions-key auth).
// App code deploys as a custom container image built from mcp-srv-func-app/mcp/Dockerfile.

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

@description('ACR login server for registry identity, e.g. wx1116prodacr.azurecr.io.')
param acrLoginServer string

@description('Image the deploy workflow last pushed, read back by modules/existing-container-app.bicep. Empty on first provision, which falls back to the placeholder.')
param existingImage string = ''

@description('Env vars currently on the live app. Names this module does not own are carried forward.')
param existingEnv array = []

@secure()
@description('Secrets currently on the live app as { list: [{ name, value }] }. Carried forward verbatim; this module never authors secrets itself.')
param existingSecrets object = {}

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

// First create only, until the first ACR image deploy lands. The bare Functions
// host image starts and serves on port 80 with no functions loaded, so the app
// comes up healthy instead of crash-looping. Must match the net10.0 worker.
var placeholderImage = 'mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0'

var provisionEnvVars = [
  {
    name: 'FUNCTIONS_EXTENSION_VERSION'
    value: '~4'
  }
  {
    name: 'FUNCTIONS_WORKER_RUNTIME'
    value: 'dotnet-isolated'
  }
  {
    name: 'AZURE_FUNCTIONS_ENVIRONMENT'
    value: 'Production'
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

var provisionEnvNames = map(provisionEnvVars, envVar => envVar.name)

// The Functions host settings above stay owned by provision; the build metadata
// prod-deploy-mcp-srv-func.yml adds (BUILD_NUMBER and friends) has to survive
// this PUT.
var envVars = concat(provisionEnvVars, filter(existingEnv, envVar => !contains(provisionEnvNames, envVar.name)))

var image = empty(existingImage) ? placeholderImage : existingImage

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

// 2024-03-01 silently ignores `kind`, deploying a plain container app that
// `az containerapp function keys` then rejects with "is not an Azure
// Functions on Container App" -- kind: 'functionapp' only takes effect from
// 2024-10-02-preview onward (matches the Azure/azure-functions-on-container-apps
// sample templates).
resource functionContainerApp 'Microsoft.App/containerApps@2024-10-02-preview' = {
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
      secrets: existingSecrets.?list ?? []
      registries: [
        {
          server: acrLoginServer
          identity: userAssignedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: name
          image: image
          env: envVars
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
