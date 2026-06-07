-- ============================================================
-- PostgreSQL Replication Setup (PC1 Master)
-- Creates replication user that PC2 will use
-- ============================================================

-- Create replication role (IF NOT EXISTS avoids errors on restart)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'replication_user') THEN
        CREATE ROLE replication_user WITH REPLICATION ENCRYPTED PASSWORD 'replication_password' LOGIN;
    END IF;
END
$$;

-- Grant replication permissions
GRANT CONNECT ON DATABASE chatapp TO replication_user;
