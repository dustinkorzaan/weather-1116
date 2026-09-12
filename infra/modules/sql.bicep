// SQL logical server + database. Microsoft Entra-only authentication is
// enabled from creation, so no SQL-native login can ever authenticate --
// only Entra ID can, whether that's the Entra admin (the GitHub Actions
// identity itself rather than a human, see infra/main.bicep for why) or an
// app's own managed identity via a contained user. ARM still requires an
// administratorLogin/administratorLoginPassword pair to create the server;
// both are generated internally below and never surfaced, since
// Entra-only auth makes them permanently unusable regardless of value --
// there is nothing to manage or protect. The Entra admin identity's own
// Entra-authenticated connection is what bootstraps the contained users for
// api/mvc/worker's managed identities (infra/scripts/create-contained-users.sql)
// -- Azure SQL only allows creating "FROM EXTERNAL PROVIDER" users over an
// Entra-authenticated connection. That script is run manually, once, as the
// Entra admin, not by prod-provision-infra.yml -- see that workflow's own
// comment for why.

@description('Name of the SQL logical server, e.g. wx1116-prod-sql-srv.')
param serverName string

@description('Name of the SQL database, e.g. wx1116-prod-sql-database.')
param databaseName string

param location string

@description('SQL admin login username. Auto-generated from non-secret inputs -- Entra-only authentication below makes it permanently unusable regardless of value, so there is nothing here worth marking @secure().')
param administratorLogin string = 'sqladmin${uniqueString(resourceGroup().id, serverName)}'

@secure()
@description('SQL admin login password. Auto-generated fresh on every deployment and never surfaced, for the same reason.')
param administratorLoginPassword string = newGuid()

@description('Principal ID of the Entra admin (the GitHub Actions managed identity) -- used only to bootstrap the contained users for the SQL-touching apps.')
param entraAdminPrincipalId string

@description('Login/display name of that same Entra admin.')
param entraAdminLoginName string

@description('Database SKU tier/name. Default Basic (5 DTU).')
param databaseSkuName string = 'Basic'
param databaseSkuTier string = 'Basic'

@description('Max database size in bytes. Default 2 GiB.')
param maxSizeBytes int = 2147483648

@description('Backup storage redundancy. Default Local (locally-redundant backup storage).')
@allowed([
  'Local'
  'Zone'
  'Geo'
  'GeoZone'
])
param backupStorageRedundancy string = 'Local'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: serverName
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlEntraAdmin 'Microsoft.Sql/servers/administrators@2023-08-01' = {
  parent: sqlServer
  name: 'ActiveDirectory'
  properties: {
    administratorType: 'ActiveDirectory'
    login: entraAdminLoginName
    sid: entraAdminPrincipalId
    tenantId: subscription().tenantId
  }
}

// Disables SQL-native authentication entirely -- the administratorLogin
// above becomes permanently unusable from here on, regardless of its
// value. Must come after the Entra admin exists: Azure rejects enabling
// Entra-only auth on a server with no Entra admin configured.
resource sqlAzureADOnlyAuth 'Microsoft.Sql/servers/azureADOnlyAuthentications@2023-08-01' = {
  parent: sqlServer
  name: 'Default'
  properties: {
    azureADOnlyAuthentication: true
  }
  dependsOn: [
    sqlEntraAdmin
  ]
}

// Special-cased firewall rule: 0.0.0.0-0.0.0.0 is not a literal IP, it's
// the documented Azure SQL convention for "allow any Azure-hosted
// resource" (in any subscription, not just this one) -- functionally the
// same rule the Portal creates when you check "Allow Azure services and
// resources to access this server" (named AllowAllWindowsAzureIps there),
// just renamed to avoid "WINDOWS" as a reserved word in ARM validation.
// Coarse but necessary for api/mvc/worker to reach the database at all
// today. Individual developer/office IP rules are deliberately NOT
// managed here -- add those manually (Portal/az cli) as needed, not
// through source control: they're personal and change often, and
// Azure's own Activity Log already audits who added/removed one and
// when, which fits that churn better than a git history that would
// otherwise carry personal IPs forever. The real fix for this rule's
// breadth is Private Endpoint + VNet integration, removing public network
// access entirely -- a good candidate for a future hardening pass.
resource allowAzureServicesFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: databaseSkuName
    tier: databaseSkuTier
  }
  properties: {
    maxSizeBytes: maxSizeBytes
    requestedBackupStorageRedundancy: backupStorageRedundancy
  }
}

output serverName string = sqlServer.name
output serverFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
