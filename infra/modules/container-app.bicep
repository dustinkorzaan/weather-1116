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

@description('Minimum replicas. 0 lets every app scale to zero between requests.')
param minReplicas int = 0

@description('Maximum replicas.')
param maxReplicas int = 3

@description('KEDA cooldown period in seconds before scaling in to minReplicas. 1800 (30 min) trades a longer window of an idle-but-warm replica for fewer cold starts than the 300s platform default; ACA caps this at 3600.')
param cooldownPeriod int = 1800

@description('Daily window, in scheduledWarmTimezone, during which a cron scale rule holds at least one replica even with no traffic. Covers the worker\'s 11:00 UTC import-cities job, which only runs if a replica is up when Hangfire ticks.')
param scheduledWarmStart string = '0 11 * * *'

param scheduledWarmEnd string = '30 13 * * *'

param scheduledWarmTimezone string = 'Etc/UTC'

@description('Enable sticky sessions for Blazor Server SignalR.')
param stickySessions bool = false

@description('Resource ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityId string

@description('Client ID of this app\'s dedicated User-Assigned Managed Identity.')
param userAssignedIdentityClientId string

@description('vCPU per replica, as a string (Bicep has no decimal literal). Must pair with memory per ACA Consumption sizes.')
param cpu string = '0.25'

@description('Memory per replica, e.g. 0.5Gi. Must pair with cpu per ACA Consumption sizes (memory = 2 x cpu).')
param memory string = '0.5Gi'

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

resource containerApp 'Microsoft.App/containerApps@2025-01-01' = {
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
            cpu: json(cpu)
            memory: memory
          }
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        cooldownPeriod: cooldownPeriod
        // Declaring any rule drops ACA's implicit HTTP rule, so it is restated
        // here with the platform default of 10 concurrent requests.
        rules: [
          {
            name: 'http-scale'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
          {
            name: 'scheduled-warm'
            custom: {
              type: 'cron'
              metadata: {
                timezone: scheduledWarmTimezone
                start: scheduledWarmStart
                end: scheduledWarmEnd
                desiredReplicas: '1'
              }
            }
          }
        ]
      }
    }
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
