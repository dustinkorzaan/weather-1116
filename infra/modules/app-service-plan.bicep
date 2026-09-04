// Shared Linux App Service Plan hosting all 6 apps (5 web apps + the
// Function App, which is Dedicated rather than Flex Consumption). Basic B2
// by default -- worker and the Function App both need alwaysOn, which
// Free/Shared tiers don't support.

@description('Name of the App Service Plan, e.g. wx1116-prd-asp.')
param name string

param location string

@description('App Service Plan SKU. Default B2 -- bump to B3/S1 if 6 apps get tight on capacity, since there is no scale-to-zero fallback for the Function App on a Dedicated plan.')
param skuName string = 'B2'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  kind: 'linux'
  sku: {
    name: skuName
  }
  properties: {
    reserved: true
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name
