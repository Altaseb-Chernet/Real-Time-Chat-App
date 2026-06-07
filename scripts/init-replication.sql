-- ============================================================
-- PostgreSQL Replication Setup (PC1 Master)
-- Creates replication user that PC2 will use
-- ============================================================

-- Create replication role
CREATE ROLE replication_user WITH REPLICATION ENCRYPTED PASSWORD 'replication_password' LOGIN;

-- Grant replication permissions
GRANT CONNECT ON DATABASE chatapp TO replication_user;

-- Configure pg_hba.conf for replication connections
-- This allows PC2 to connect and stream WAL logs
-- Also allow normal connections from any IP so PC2 API can connect directly
-- We use a system execution trick to append to pg_hba.conf if needed, 
-- but in docker postgres it's better to configure it via command line or a script.
-- For this simple setup, we'll rely on the default docker postgres behavior 
-- which usually allows connections from any IP if POSTGRES_PASSWORD is set,
-- but we explicitly add replication permissions.

COPY (SELECT 'host replication all 0.0.0.0/0 md5') TO '/var/lib/postgresql/data/pg_hba.conf' WITH (FORMAT text, HEADER false);
COPY (SELECT 'host all all 0.0.0.0/0 md5') TO '/var/lib/postgresql/data/pg_hba.conf' WITH (FORMAT text, HEADER false);
-- We need to reload the config for pg_hba.conf changes to take effect
SELECT pg_reload_conf();

COMMIT;
