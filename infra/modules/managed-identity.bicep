// References the GitHub Actions managed identity you create manually (along with
// its federated credential for GitHub OIDC and a one-time Owner grant on the
// resource group) and codifies its durable, least-privilege footprint in code:
// standing Contributor + User Access Administrator grants scoped to this resource
// group. This module never creates the identity or its federated credential —
// without both of those, GitHub Actions cannot authenticate to run provision.

@description('Name of the pre-existing GitHub Actions managed identity (e.g. wx1116-prod-github-actions-mi). Created manually, not by this template.')
param githubActionsIdentityName string

var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var userAccessAdministratorRoleId = '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9'

resource githubActionsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: githubActionsIdentityName
}

resource contributorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, githubActionsIdentity.id, contributorRoleId)
  properties: {
    principalId: githubActionsIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
  }
}

resource userAccessAdministratorAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, githubActionsIdentity.id, userAccessAdministratorRoleId)
  properties: {
    principalId: githubActionsIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', userAccessAdministratorRoleId)
  }
}

output principalId string = githubActionsIdentity.properties.principalId
output clientId string = githubActionsIdentity.properties.clientId
