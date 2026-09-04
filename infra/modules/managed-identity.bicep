// References the GitHub Actions managed identity you create manually
// (along with its one-time Owner grant on the resource group) and codifies
// its durable, least-privilege footprint in code: a federated credential for
// GitHub OIDC, plus a standing Contributor + User Access Administrator grant
// scoped to this resource group. This module never creates the identity
// itself.

@description('Name of the pre-existing GitHub Actions managed identity (e.g. wx1116-prod-github-actions-mi). Created manually, not by this template.')
param githubActionsIdentityName string

@description('GitHub repository in owner/repo form, used to build the federated credential subject.')
param githubRepository string

@description('GitHub Environment name used by the deploy jobs (matches `environment:` in the workflow YAML).')
param githubEnvironmentName string = 'prod'

var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var userAccessAdministratorRoleId = '18d7d88d-d35e-4fb5-a5c3-7773c20a72d9'

resource githubActionsIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: githubActionsIdentityName
}

resource federatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubActionsIdentity
  name: 'github-actions-${githubEnvironmentName}'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepository}:environment:${githubEnvironmentName}'
    audiences: [
      'api://AzureADTokenExchange'
    ]
  }
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
