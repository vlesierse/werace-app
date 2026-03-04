# Database Seed Data

This directory holds the Jolpica database dump file used to populate the WeRace PostgreSQL database.

## Getting the Jolpica Dump

1. Visit the [Jolpica F1 database](https://github.com/jolpica/jolpica-f1) repository.
2. Download the latest MySQL dump file from the releases page.
3. Place the `.sql` file in this directory (for example, `jolpica-dump.sql`).

The dump is a MySQL-dialect SQL file containing `INSERT INTO` statements for all historical F1 data (1950 to present).

## Running the Import

From the repository root:

```bash
dotnet run --project src/api/WeRace.DataImport -- \
    --source db/seed/jolpica-dump.sql \
    --connection "Host=localhost;Database=werace;Username=postgres;Password=yourpassword" \
    --mode full
```

### Import Modes

| Mode | Behavior |
|------|----------|
| `full` | Truncates all tables and reloads from the dump file. Use for initial seeding. |
| `delta` | Upserts rows (insert or update on conflict). Use for incremental updates. |

## After Import

The importer runs post-import validation automatically, checking row counts, foreign key integrity, data range (seasons 1950 to present), and known data point spot checks.

To apply database views and roles after seeding:

```bash
psql -h localhost -U postgres -d werace -f db/views.sql
psql -h localhost -U postgres -d werace -f db/roles.sql
```
