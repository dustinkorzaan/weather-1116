// Static Web App, deliberately provisioned without repositoryUrl/branch/
// buildProperties -- setting those would make Azure try to manage its own
// GitHub-integrated workflow, fighting the existing hand-written
// prod-deploy-react.yml pattern. After provisioning,
// retrieve the deployment token manually (az staticwebapp secrets list)
// and add it as a GitHub secret for a future deploy workflow.

@description('Name of the Static Web App, e.g. wx1116-prod-react.')
param name string

param location string

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

output id string = staticWebApp.id
output name string = staticWebApp.name
output defaultHostname string = staticWebApp.properties.defaultHostname
