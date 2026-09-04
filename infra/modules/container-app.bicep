// Reusable Linux container app module for ASP.NET workloads.

@description('Container app name, e.g. wx1116-prod-api.')
param name string

param location string

@description('Resource ID of the Container Apps Environment.')
param managedEnvironmentId string

@description('Full image reference, e.g. mcr.microsoft.com/dotnet/aspnet:10.0 for the initial placeholder.')
param containerImage string

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

var envVars = setAzureClientId ? concat(baseEnvVars, clientIdEnvVar) : baseEnvVars

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
          image: containerImage
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
