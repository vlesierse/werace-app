# Database Seed Data

This directory is where the Jolpica F1 CSV files should be placed before running the data import. The CSV files are **not committed** to the repository (`.gitignore`d) and must be obtained separately.

## Obtaining the Seed Data

1. Visit the [Jolpica F1 database](https://github.com/jolpica/jolpica-f1) repository.
2. Download the latest CSV export from the [releases page](https://github.com/jolpica/jolpica-f1/releases).
3. Extract and copy all `formula_one_*.csv` files into this directory (`db/seed/`).

After copying, the directory should contain the following files:

| File | Description |
|------|-------------|
| `formula_one_season.csv` | F1 seasons (1950–present) |
| `formula_one_circuit.csv` | Circuit metadata (location, coordinates) |
| `formula_one_driver.csv` | Driver profiles |
| `formula_one_round.csv` | Race weekends per season |
| `formula_one_session.csv` | Sessions per round (FP1, Q1–Q3, Sprint, Race) |
| `formula_one_team.csv` | Constructor/team data |
| `formula_one_baseteam.csv` | Base team identities |
| `formula_one_teamdriver.csv` | Driver–team assignments per season |
| `formula_one_roundentry.csv` | Entries per round (car numbers) |
| `formula_one_sessionentry.csv` | Per-driver session results |
| `formula_one_lap.csv` | Individual lap data |
| `formula_one_pitstop.csv` | Pit stop records |
| `formula_one_penalty.csv` | Penalty records |
| `formula_one_pointsystem.csv` | Points scoring systems |
| `formula_one_driverchampionship.csv` | Driver championship standings |
| `formula_one_teamchampionship.csv` | Constructor championship standings |
| `formula_one_championshipsystem.csv` | Championship calculation rules |
| `formula_one_championshipadjustment.csv` | Manual championship adjustments |

## Running the Import

From the repository root:

```bash
dotnet run --project src/api/WeRace.DataImport -- \
    --source db/seed \
    --connection "Host=localhost;Database=werace;Username=postgres;Password=yourpassword" \
    --mode full
```

### Import Modes

| Mode | Behavior |
|------|----------|
| `full` | Truncates all tables and reloads from the CSV directory. Use for initial seeding. |
| `delta` | Upserts rows (insert or update on conflict). Use for incremental updates. |

### What the Importer Does

1. Parses all `formula_one_*.csv` files in the source directory
2. Maps the Jolpica normalized data model to the WeRace PostgreSQL schema (14 tables)
3. Resolves FK chains (session → round, sessionentry → roundentry → teamdriver)
4. Bulk loads via PostgreSQL COPY
5. Resets SERIAL sequences
6. Runs post-import validation (row counts, FK integrity, data range, spot checks)

## After Import

Apply database views and roles after seeding:

```bash
psql -h localhost -U postgres -d werace -f db/views.sql
psql -h localhost -U postgres -d werace -f db/roles.sql
```
