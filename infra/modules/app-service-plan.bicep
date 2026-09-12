// Shared Linux App Service Plan for all wx1116-prod web apps and the
// Function App. One plan, one instance, all apps share its compute.

@description('Name of the App Service Plan, e.g. wx1116-prod-asp.')
param name string

param location string

@description('App Service Plan SKU name, e.g. B1.')
param skuName string = 'B1'

@description('App Service Plan SKU tier, e.g. Basic.')
param skuTier string = 'Basic'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

output id string = plan.id
output name string = plan.name
