# Decision: Data Pipeline Implementation Approach

**By:** Gilfoyle (Backend Developer)
**Date:** 2026-03-04
**Context:** E2 Data Pipeline — WeRace.DataImport CLI tool and db/ SQL files

## Decisions Made

### 1. PostgreSQL text COPY for bulk loading (not binary)

**What:** The import tool uses `COPY FROM STDIN (FORMAT text)` instead of `NpgsqlBinaryImporter`.
**Why:** Text COPY handles NULL values and type coercion more gracefully for string-heavy dump data. Binary COPY requires exact type matching at the CLR level, which adds fragility when parsing MySQL dump values. Text COPY is still orders of magnitude faster than individual INSERTs. Performance difference vs. binary is negligible for our data volume (~500K rows across all tables).

### 2. Delta mode uses temp table + INSERT ON CONFLICT

**What:** Delta (upsert) mode creates a temp table, COPYs data in, then runs `INSERT ... ON CONFLICT DO UPDATE`.
**Why:** Avoids row-by-row upsert loops. Single bulk operation. Temp table is auto-dropped on commit. This approach handles both new rows and updated rows in one pass.

### 3. System.CommandLine beta5 for CLI parsing

**What:** Using `System.CommandLine 2.0.0-beta5.25306.1` (latest prerelease).
**Why:** The stable 2.x hasn't shipped yet. Beta5 has a significantly different API from beta4 (no `SetHandler`, uses `SetAction` + `ParseResult.GetValue`). Documented in history for future reference.

### 4. db/ directory as schema source of truth

**What:** `db/schema.sql` is the canonical DDL, not EF Core migrations.
**Why:** EF Core migrations will be generated from the DbContext, but the raw SQL in `db/` is human-readable, reviewable, and can be applied directly to PostgreSQL without .NET. Useful for DBA review, CI/CD, and AI role/view setup.

### 5. Manual seed for now, Aspire automation deferred

**What:** AppHost has a TODO comment for future Aspire seed automation. Current approach is manual CLI invocation.
**Why:** Aspire doesn't natively support "run to completion" project references (one-shot tasks). Adding custom lifecycle hooks adds complexity we don't need yet. The CLI is simple and works.
