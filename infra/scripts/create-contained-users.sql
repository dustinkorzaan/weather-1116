-- Creates contained database users for the api/mvc/worker/mcp-srv-app-service managed
-- identities and grants db_owner, plus a read-only (db_datareader) user for the
-- mcp-srv-func-app identity (GetCities only reads dbo.City), so those apps can connect to SQL via
-- Entra-integrated auth (no password anywhere). Must be run over an
-- Entra-authenticated connection -- Azure SQL does not allow
-- CREATE USER ... FROM EXTERNAL PROVIDER over a SQL-native login,
-- regardless of that login's privileges.
--
-- Idempotent: safe to re-run on every provision. CREATE USER is guarded by
-- an existence check; ALTER ROLE ADD MEMBER is naturally a no-op when the
-- principal is already a member.

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'wx1116-prod-api-mi')
BEGIN
    CREATE USER [wx1116-prod-api-mi] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_owner ADD MEMBER [wx1116-prod-api-mi];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'wx1116-prod-mvc-mi')
BEGIN
    CREATE USER [wx1116-prod-mvc-mi] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_owner ADD MEMBER [wx1116-prod-mvc-mi];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'wx1116-prod-worker-mi')
BEGIN
    CREATE USER [wx1116-prod-worker-mi] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_owner ADD MEMBER [wx1116-prod-worker-mi];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'wx1116-prod-mcp-srv-app-service-mi')
BEGIN
    CREATE USER [wx1116-prod-mcp-srv-app-service-mi] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_owner ADD MEMBER [wx1116-prod-mcp-srv-app-service-mi];

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'wx1116-prod-mcp-srv-func-app-mi')
BEGIN
    CREATE USER [wx1116-prod-mcp-srv-func-app-mi] FROM EXTERNAL PROVIDER;
END
ALTER ROLE db_datareader ADD MEMBER [wx1116-prod-mcp-srv-func-app-mi];
