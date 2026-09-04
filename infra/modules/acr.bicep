// Azure Container Registry for all six container apps, including the
// Functions-on-ACA MCP host (Functions on ACA deploys custom images, not packages).

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
