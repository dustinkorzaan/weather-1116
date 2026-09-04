// Azure Container Registry for the five ASP.NET container apps.
// Functions-on-ACA deploys via dotnet publish, not custom images from this registry.

@description('Globally unique ACR name, e.g. wx1116prodacr (alphanumeric only, 5-50 chars).')
param name string

param location string

@description('ACR SKU. Basic is sufficient for this sample.')
param skuName string = 'Basic'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  sku: {
    name: skuName
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

output id string = registry.id
output name string = registry.name
output loginServer string = registry.properties.loginServer
