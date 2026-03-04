-- ============================================
-- WeRace Database Roles
-- AI readonly role for Phase 2 SQL query generation.
-- This role can SELECT from all F1 data tables but cannot modify data.
-- ============================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'werace_ai_readonly') THEN
        CREATE ROLE werace_ai_readonly LOGIN PASSWORD 'readonly_changeme';
    END IF;
END
$$;

-- Grant usage on public schema
GRANT USAGE ON SCHEMA public TO werace_ai_readonly;

-- Grant SELECT on all existing tables
GRANT SELECT ON ALL TABLES IN SCHEMA public TO werace_ai_readonly;

-- Grant SELECT on all future tables (so new tables are automatically accessible)
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO werace_ai_readonly;

-- Explicitly deny write operations (defense-in-depth)
-- PostgreSQL denies by default, but be explicit
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON ALL TABLES IN SCHEMA public FROM werace_ai_readonly;

-- Set statement timeout for safety (5 seconds max query time)
ALTER ROLE werace_ai_readonly SET statement_timeout = '5s';

-- Set row limit via session variable (enforced in SQL validation middleware)
ALTER ROLE werace_ai_readonly SET work_mem = '64MB';
