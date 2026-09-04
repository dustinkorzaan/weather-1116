// SQL logical server + database, hybrid authentication: a SQL
// login/password (break-glass/admin-tool credential only -- no app's
// connection string carries it day-to-day) *and* an Entra admin.
// Deliberately no azureADOnlyAuthentications resource -- that's the one
// resource that would flip this from hybrid to Entra-only.

@description('Name of the SQL logical server, e.g. wx1116-prd-sql-server.')
param serverName string

@description('Name of the SQL database, e.g. wx1116-prd-sql-database.')
param databaseName string

param location string

@description('SQL login administrator username. Break-glass credential only -- no app connects with it.')
param administratorLogin string

@secure()
@description('SQL login administrator password. Supply via azd env set / --parameters at deploy time, never committed.')
param administratorLoginPassword string

@description('Login name (UPN or group name) of the Entra admin -- a user or group, your call.')
param sqlAdminLoginName string

@description('Object ID of the Entra admin principal above.')
param sqlAdminObjectId string

@description('Database SKU tier/name. Default Standard S0 -- Hangfire\'s schema/indexes likely exceed Basic\'s 5 DTU.')
param databaseSkuName string = 'S0'
param databaseSkuTier string = 'Standard'

@description('Max database size in bytes. Default 2 GiB.')
param maxSizeBytes int = 2147483648

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
    login: sqlAdminLoginName
    sid: sqlAdminObjectId
    tenantId: subscription().tenantId
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
  }
}

output serverName string = sqlServer.name
output serverFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
