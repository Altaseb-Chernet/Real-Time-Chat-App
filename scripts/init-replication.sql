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
-- Typically this is added to pg_hba.conf, but we can use alternative methods

COMMIT;
