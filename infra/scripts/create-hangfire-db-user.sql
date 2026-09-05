-- Optional: create a dedicated Hangfire application role (password auth).
-- Run once against wx1116-prod-hangfire as the PostgreSQL admin, e.g.:
--   psql "host=wx1116-prod-postgres.postgres.database.azure.com port=5432 dbname=wx1116-prod-hangfire user=<admin> sslmode=require" -f infra/scripts/create-hangfire-db-user.sql
--
-- Replace the placeholder password before running, then set
-- AZURE_POSTGRES_DB_CONNECTION_STRING to an Npgsql connection string using
-- this role (see docs/aca-bootstrap.md).

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hangfire_app') THEN
    CREATE ROLE hangfire_app LOGIN PASSWORD 'REPLACE_WITH_A_STRONG_PASSWORD';
  END IF;
END
$$;

GRANT CONNECT ON DATABASE "wx1116-prod-hangfire" TO hangfire_app;
GRANT USAGE, CREATE ON SCHEMA public TO hangfire_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO hangfire_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO hangfire_app;
