// Creates a single User-Assigned Managed Identity for one app's runtime
// identity. Invoked once per app (6 times) from main.bicep. Ordinary Bicep
// resource -- no bootstrap problem, since the GitHub Actions identity that
// runs this template already has Contributor on the resource group.

@description('Name of the managed identity to create, e.g. wx1116-prod-mi-api.')
param name string

param location string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
}

output id string = identity.id
output principalId string = identity.properties.principalId
output clientId string = identity.properties.clientId
