// Reusable Linux container app module for ASP.NET workloads.

@description('Container app name, e.g. wx1116-prod-api.')
param name string

param location string

@description('Resource ID of the Container Apps Environment.')
param managedEnvironmentId string

@description('Full image reference used on first create only, before any deploy has pushed an image.')
param containerImage string

@description('Image the deploy workflow last pushed, read back by modules/existing-container-app.bicep. Empty on first provision, which falls back to containerImage.')
param existingImage string = ''

@description('Env vars currently on the live app. Names this module does not own are carried forward.')
param existingEnv array = []

@secure()
@description('Secrets currently on the live app as { list: [{ name, value }] }. Carried forward verbatim; this module never authors secrets itself.')
param existingSecrets object = {}

@description('Container port. ASP.NET apps listen on 8080.')
param targetPort int = 8080

@description('Minimum replicas. Use 1 for always-on workloads (worker, blazor).')
param minReplicas int = 0

@description('Maximum replicas.')
param maxReplicas int = 3

@description('Enable sticky sessions for Blazor Server SignalR.')
param stickySessions bool = false

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityClientId string

@description('Whether to set AZURE_CLIENT_ID so DefaultAzureCredential picks this UAMI.')
param setAzureClientId bool = false

@description('ACR login server for registry identity, e.g. wx1116prodacr.azurecr.io.')
param acrLoginServer string

param appInsightsConnectionString string

var baseEnvVars = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
]

var clientIdEnvVar = [
  {
    name: 'AZURE_CLIENT_ID'
    value: userAssignedIdentityClientId
  }
]

var provisionEnvVars = setAzureClientId ? concat(baseEnvVars, clientIdEnvVar) : baseEnvVars
var provisionEnvNames = map(provisionEnvVars, envVar => envVar.name)

// Names above stay owned by provision, so changes to them (a rotated App
// Insights connection string, say) still land. Everything else on the live app
// was set by prod-deploy-*.yml and has to survive this PUT.
var envVars = concat(provisionEnvVars, filter(existingEnv, envVar => !contains(provisionEnvNames, envVar.name)))

var image = empty(existingImage) ? containerImage : existingImage

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
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
        targetPort: targetPort
        transport: 'auto'
        allowInsecure: false
        stickySessions: {
          affinity: stickySessions ? 'sticky' : 'none'
        }
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
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
