# Technical Foundation — WeRace Phase 1

**Author:** Gilfoyle (Backend Developer)
**Date:** 2026-02-26
**Status:** Technical blueprint for Phase 1 implementation
**Scope:** Data browsing + core navigation + authentication (4-6 weeks)

---

## 1. Solution Structure

### .NET Solution Layout

```
werace-app/
├── WeRace.sln
├── src/
│   ├── WeRace.AppHost/                  # Aspire orchestrator
│   │   ├── Program.cs
│   │   └── WeRace.AppHost.csproj
│   ├── WeRace.ServiceDefaults/          # Aspire shared config (telemetry, health checks)
│   │   ├── Extensions.cs
│   │   └── WeRace.ServiceDefaults.csproj
│   ├── WeRace.Api/                      # Minimal API host — endpoints, middleware, DI
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   ├── SeasonEndpoints.cs
│   │   │   ├── RaceEndpoints.cs
│   │   │   ├── DriverEndpoints.cs
│   │   │   ├── ConstructorEndpoints.cs
│   │   │   ├── CircuitEndpoints.cs
│   │   │   └── AuthEndpoints.cs
│   │   ├── Middleware/
│   │   │   └── RateLimitingMiddleware.cs
│   │   ├── WeRace.Api.csproj
│   │   └── appsettings.json
│   ├── WeRace.Domain/                   # Entities, enums, value objects — zero dependencies
│   │   ├── Entities/
│   │   │   ├── Season.cs
│   │   │   ├── Race.cs
│   │   │   ├── Circuit.cs
│   │   │   ├── Driver.cs
│   │   │   ├── Constructor.cs
│   │   │   ├── Result.cs
│   │   │   ├── Qualifying.cs
│   │   │   ├── PitStop.cs
│   │   │   ├── SprintResult.cs
│   │   │   ├── DriverStanding.cs
│   │   │   ├── ConstructorStanding.cs
│   │   │   ├── LapTime.cs
│   │   │   └── Status.cs
│   │   └── WeRace.Domain.csproj
│   ├── WeRace.Infrastructure/           # EF Core DbContext, migrations, data access
│   │   ├── Data/
│   │   │   ├── WeRaceDbContext.cs
│   │   │   └── Configurations/          # EF Core entity configurations
│   │   │       ├── SeasonConfiguration.cs
│   │   │       ├── RaceConfiguration.cs
│   │   │       └── ...
│   │   ├── Migrations/
│   │   ├── Repositories/                # Query-specific data access
│   │   └── WeRace.Infrastructure.csproj
│   └── WeRace.DataImport/              # CLI tool for Jolpica dump import
│       ├── Program.cs
│       ├── Importers/
│       │   ├── JolpicaDumpImporter.cs
│       │   └── SchemaMapper.cs
│       └── WeRace.DataImport.csproj
├── tests/
│   ├── WeRace.Api.Tests/               # Endpoint integration tests
│   ├── WeRace.Infrastructure.Tests/     # Data access tests
│   └── WeRace.DataImport.Tests/         # Import pipeline tests
└── db/
    ├── schema.sql                       # Full DDL (source of truth)
    ├── views.sql                        # Database views (AI foundation)
    ├── roles.sql                        # Database roles (werace_ai_readonly)
    └── seed/                            # Jolpica dump import scripts
        └── import.sh
```

### Dependency Direction

```
WeRace.AppHost
    → WeRace.Api
    → WeRace.ServiceDefaults

WeRace.Api
    → WeRace.Domain
    → WeRace.Infrastructure
    → WeRace.ServiceDefaults

WeRace.Infrastructure
    → WeRace.Domain

WeRace.DataImport
    → WeRace.Domain
    → WeRace.Infrastructure

WeRace.Domain
    → (nothing — zero external dependencies)
```

**Rules:**

- `Domain` has zero NuGet dependencies. No EF Core attributes on entities. POCO only.
- `Infrastructure` owns all EF Core configuration via `IEntityTypeConfiguration<T>` — entities stay clean.
- `Api` owns DI registration, endpoint mapping, and middleware. No business logic.
- `DataImport` is a standalone CLI tool. Not deployed with the API. Runs once for seeding, supports re-runs.

### Naming Conventions

| Artifact | Convention | Example |
|----------|-----------|---------|
| Projects | `WeRace.{Layer}` | `WeRace.Api`, `WeRace.Domain` |
| Entities | PascalCase, singular | `Driver`, `ConstructorStanding` |
| DB tables | snake_case, plural | `drivers`, `constructor_standings` |
| DB columns | snake_case | `driver_ref`, `date_of_birth` |
| API routes | kebab-case, plural | `/api/v1/drivers`, `/api/v1/pit-stops` |
| C# files | PascalCase matching class | `DriverEndpoints.cs` |

### Aspire AppHost Configuration

```csharp
// src/WeRace.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure resources
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("werace-pgdata");

var db = postgres.AddDatabase("werace");

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

// API project
var api = builder.AddProject<Projects.WeRace_Api>("api")
    .WithReference(db)
    .WithReference(cache)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

**What Aspire provides:**

- PostgreSQL + pgAdmin for database management during development
- Redis + RedisInsight for caching (response cache, rate limiting counters)
- Automatic connection string injection via `builder.AddPostgres` → no manual config
- Health checks and OpenTelemetry wired through `WeRace.ServiceDefaults`
- Service discovery for future microservice decomposition (Phase 2 AI service)

---

## 2. Database Schema (Phase 1)

### Design Principles

1. **Jolpica-aligned** — Column names and table structure track the Ergast/Jolpica dump schema to minimize transformation during import.
2. **Snake case everywhere** — PostgreSQL convention. EF Core maps via `NpgsqlSnakeCaseNamingConvention`.
3. **Composite keys where natural** — `lap_times`, `pit_stops` use composite PKs matching source data.
4. **Nullable where historically sparse** — Pre-2000 data has gaps. Columns are nullable when 1950s–1970s data might be missing.

### Full DDL

```sql
-- ============================================
-- WeRace Phase 1 Schema — PostgreSQL
-- ============================================

-- Seasons
CREATE TABLE seasons (
    id              SERIAL PRIMARY KEY,
    year            INT NOT NULL UNIQUE,
    wikipedia_url   TEXT
);

CREATE INDEX idx_seasons_year ON seasons (year);

-- Circuits
CREATE TABLE circuits (
    id              SERIAL PRIMARY KEY,
    circuit_ref     VARCHAR(255) NOT NULL UNIQUE,
    name            VARCHAR(255) NOT NULL,
    location        VARCHAR(255),
    country         VARCHAR(255),
    latitude        DECIMAL(10, 6),
    longitude       DECIMAL(10, 6),
    altitude        INT,
    wikipedia_url   TEXT
);

CREATE INDEX idx_circuits_circuit_ref ON circuits (circuit_ref);

-- Races
CREATE TABLE races (
    id              SERIAL PRIMARY KEY,
    season_id       INT NOT NULL REFERENCES seasons(id),
    round           INT NOT NULL,
    name            VARCHAR(255) NOT NULL,
    circuit_id      INT NOT NULL REFERENCES circuits(id),
    date            DATE NOT NULL,
    time            TIME,
    fp1_date        DATE,
    fp1_time        TIME,
    fp2_date        DATE,
    fp2_time        TIME,
    fp3_date        DATE,
    fp3_time        TIME,
    quali_date      DATE,
    quali_time      TIME,
    sprint_date     DATE,
    sprint_time     TIME,
    wikipedia_url   TEXT,

    UNIQUE (season_id, round)
);

CREATE INDEX idx_races_season_id ON races (season_id);
CREATE INDEX idx_races_circuit_id ON races (circuit_id);
CREATE INDEX idx_races_date ON races (date);

-- Drivers
CREATE TABLE drivers (
    id              SERIAL PRIMARY KEY,
    driver_ref      VARCHAR(255) NOT NULL UNIQUE,
    number          INT,
    code            VARCHAR(3),
    forename        VARCHAR(255) NOT NULL,
    surname         VARCHAR(255) NOT NULL,
    date_of_birth   DATE,
    nationality     VARCHAR(255),
    wikipedia_url   TEXT
);

CREATE INDEX idx_drivers_driver_ref ON drivers (driver_ref);
CREATE INDEX idx_drivers_code ON drivers (code);

-- Constructors
CREATE TABLE constructors (
    id                  SERIAL PRIMARY KEY,
    constructor_ref     VARCHAR(255) NOT NULL UNIQUE,
    name                VARCHAR(255) NOT NULL,
    nationality         VARCHAR(255),
    wikipedia_url       TEXT
);

CREATE INDEX idx_constructors_constructor_ref ON constructors (constructor_ref);

-- Status (race finish statuses)
CREATE TABLE status (
    id      SERIAL PRIMARY KEY,
    status  VARCHAR(255) NOT NULL
);

-- Results (race results)
CREATE TABLE results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT,
    grid                INT NOT NULL,
    position            INT,
    position_text       VARCHAR(255) NOT NULL,
    position_order      INT NOT NULL,
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    laps                INT NOT NULL DEFAULT 0,
    time                VARCHAR(255),
    milliseconds        INT,
    fastest_lap         INT,
    rank                INT,
    fastest_lap_time    VARCHAR(255),
    fastest_lap_speed   VARCHAR(255),
    status_id           INT NOT NULL REFERENCES status(id)
);

CREATE INDEX idx_results_race_id ON results (race_id);
CREATE INDEX idx_results_driver_id ON results (driver_id);
CREATE INDEX idx_results_constructor_id ON results (constructor_id);
CREATE INDEX idx_results_race_driver ON results (race_id, driver_id);

-- Qualifying (separate entity — see recommendation in gilfoyle-data-model-recommendations.md)
CREATE TABLE qualifying (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT NOT NULL,
    position            INT NOT NULL,
    q1                  VARCHAR(255),
    q2                  VARCHAR(255),
    q3                  VARCHAR(255)
);

CREATE INDEX idx_qualifying_race_id ON qualifying (race_id);
CREATE INDEX idx_qualifying_race_driver ON qualifying (race_id, driver_id);

-- Sprint Results (separate table for 2021+ sprint races)
CREATE TABLE sprint_results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    driver_id           INT NOT NULL REFERENCES drivers(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    number              INT,
    grid                INT NOT NULL,
    position            INT,
    position_text       VARCHAR(255) NOT NULL,
    position_order      INT NOT NULL,
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    laps                INT NOT NULL DEFAULT 0,
    time                VARCHAR(255),
    milliseconds        INT,
    fastest_lap         INT,
    fastest_lap_time    VARCHAR(255),
    status_id           INT NOT NULL REFERENCES status(id)
);

CREATE INDEX idx_sprint_results_race_id ON sprint_results (race_id);
CREATE INDEX idx_sprint_results_driver_id ON sprint_results (driver_id);

-- Pit Stops
CREATE TABLE pit_stops (
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    stop            INT NOT NULL,
    lap             INT NOT NULL,
    time            TIME,
    duration        VARCHAR(255),
    milliseconds    INT,

    PRIMARY KEY (race_id, driver_id, stop)
);

CREATE INDEX idx_pit_stops_race_id ON pit_stops (race_id);
CREATE INDEX idx_pit_stops_driver_id ON pit_stops (driver_id);

-- Lap Times
CREATE TABLE lap_times (
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    lap             INT NOT NULL,
    position        INT,
    time            VARCHAR(255),
    milliseconds    INT,

    PRIMARY KEY (race_id, driver_id, lap)
);

CREATE INDEX idx_lap_times_race_id ON lap_times (race_id);
CREATE INDEX idx_lap_times_driver_id ON lap_times (driver_id);

-- Driver Standings
CREATE TABLE driver_standings (
    id              SERIAL PRIMARY KEY,
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    points          DECIMAL(5, 2) NOT NULL DEFAULT 0,
    position        INT,
    position_text   VARCHAR(255),
    wins            INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_driver_standings_race_id ON driver_standings (race_id);
CREATE INDEX idx_driver_standings_driver_id ON driver_standings (driver_id);
CREATE INDEX idx_driver_standings_race_driver ON driver_standings (race_id, driver_id);

-- Constructor Standings
CREATE TABLE constructor_standings (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    points              DECIMAL(5, 2) NOT NULL DEFAULT 0,
    position            INT,
    position_text       VARCHAR(255),
    wins                INT NOT NULL DEFAULT 0
);

CREATE INDEX idx_constructor_standings_race_id ON constructor_standings (race_id);
CREATE INDEX idx_constructor_standings_constructor_id ON constructor_standings (constructor_id);

-- Constructor Results (aggregate race-level constructor data from Jolpica)
CREATE TABLE constructor_results (
    id                  SERIAL PRIMARY KEY,
    race_id             INT NOT NULL REFERENCES races(id),
    constructor_id      INT NOT NULL REFERENCES constructors(id),
    points              DECIMAL(5, 2),
    status              VARCHAR(255)
);

CREATE INDEX idx_constructor_results_race_id ON constructor_results (race_id);
CREATE INDEX idx_constructor_results_constructor_id ON constructor_results (constructor_id);
```

### .NET Identity Integration

.NET Identity tables live in the **same PostgreSQL database** under a separate `identity` schema to keep F1 data cleanly separated.

```sql
-- Identity schema (managed by EF Core Identity migrations)
CREATE SCHEMA IF NOT EXISTS identity;

-- .NET Identity will generate these tables via migration:
-- identity.asp_net_users
-- identity.asp_net_roles
-- identity.asp_net_user_roles
-- identity.asp_net_user_claims
-- identity.asp_net_user_logins
-- identity.asp_net_user_tokens
-- identity.asp_net_role_claims

-- WeRace user profile extension (links Identity user to app-specific data)
CREATE TABLE user_profiles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    identity_id     VARCHAR(450) NOT NULL UNIQUE,  -- FK to identity.asp_net_users.id
    display_name    VARCHAR(255),
    ai_queries_today INT NOT NULL DEFAULT 0,
    ai_queries_reset_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_user_profiles_identity_id ON user_profiles (identity_id);
```

**Why separate schema, same database:**

- Single connection string. Aspire hands it to us. Done.
- Identity migrations run against `identity.*` schema; F1 data migrations run against `public.*`.
- AI readonly role (Phase 2) gets `GRANT SELECT ON ALL TABLES IN SCHEMA public` — no accidental Identity data exposure.

### EF Core Configuration

```csharp
// src/WeRace.Infrastructure/Data/WeRaceDbContext.cs
public class WeRaceDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<Circuit> Circuits => Set<Circuit>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Constructor> Constructors => Set<Constructor>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Qualifying> Qualifyings => Set<Qualifying>();
    public DbSet<SprintResult> SprintResults => Set<SprintResult>();
    public DbSet<PitStop> PitStops => Set<PitStop>();
    public DbSet<LapTime> LapTimes => Set<LapTime>();
    public DbSet<DriverStanding> DriverStandings => Set<DriverStanding>();
    public DbSet<ConstructorStanding> ConstructorStandings => Set<ConstructorStanding>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity tables

        // Identity schema separation
        modelBuilder.Entity<IdentityUser>().ToTable("asp_net_users", "identity");
        modelBuilder.Entity<IdentityRole>().ToTable("asp_net_roles", "identity");
        modelBuilder.Entity<IdentityUserRole<string>>().ToTable("asp_net_user_roles", "identity");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("asp_net_user_claims", "identity");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("asp_net_user_logins", "identity");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("asp_net_user_tokens", "identity");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("asp_net_role_claims", "identity");

        // Apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WeRaceDbContext).Assembly);
    }
}
```

```csharp
// src/WeRace.Api/Program.cs (relevant DI section)
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<WeRaceDbContext>("werace", options =>
{
    options.UseSnakeCaseNamingConvention();
});

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<WeRaceDbContext>();
```

---

## 3. Data Pipeline

### Jolpica Dump Format

The Jolpica/Ergast database dump is a **MySQL dump file** (`.sql` with `CREATE TABLE` and `INSERT INTO` statements in MySQL dialect). It covers 14 tables:

| Jolpica Table | WeRace Table | Notes |
|---------------|-------------|-------|
| `seasons` | `seasons` | Direct map |
| `circuits` | `circuits` | Direct map, add `circuit_ref` |
| `races` | `races` | Map `year` → `season_id` FK lookup |
| `drivers` | `drivers` | Direct map |
| `constructors` | `constructors` | Direct map |
| `status` | `status` | Direct map |
| `results` | `results` | Direct map |
| `qualifying` | `qualifying` | Direct map |
| `sprintResults` | `sprint_results` | Direct map, snake_case rename |
| `pitStops` | `pit_stops` | Direct map, snake_case rename |
| `lapTimes` | `lap_times` | Direct map, snake_case rename |
| `driverStandings` | `driver_standings` | Direct map, snake_case rename |
| `constructorStandings` | `constructor_standings` | Direct map, snake_case rename |
| `constructorResults` | `constructor_results` | Direct map, snake_case rename |

### Import Strategy

#### Approach: MySQL → CSV → PostgreSQL COPY

The cleanest pipeline: parse the MySQL dump to CSV, then use PostgreSQL `COPY` for bulk loading. This avoids cross-dialect SQL translation.

```
Jolpica MySQL dump (.sql)
    → mysql2csv (parse INSERT statements → CSV per table)
    → validate CSV (row counts, FK integrity check)
    → PostgreSQL COPY FROM (bulk load)
    → post-import validation
```

#### Import Tool: `WeRace.DataImport`

A .NET CLI tool (not a web app). Runs from command line.

```bash
# One-time seed
dotnet run --project src/WeRace.DataImport -- \
    --source ./db/seed/jolpica-dump.sql \
    --connection "Host=localhost;Database=werace;..." \
    --mode full

# Future delta (re-import with upsert)
dotnet run --project src/WeRace.DataImport -- \
    --source ./db/seed/jolpica-dump-2026.sql \
    --connection "Host=localhost;Database=werace;..." \
    --mode delta
```

**Implementation approach:**

```csharp
// Simplified import flow
public class JolpicaDumpImporter
{
    public async Task ImportAsync(string dumpFilePath, ImportMode mode)
    {
        // 1. Parse MySQL dump → extract INSERT statements per table
        var tables = MySqlDumpParser.Parse(dumpFilePath);

        // 2. Transform to WeRace schema (column mapping, FK resolution)
        var mapped = SchemaMapper.MapAll(tables);

        // 3. Load order respects FK dependencies
        var loadOrder = new[]
        {
            "seasons", "circuits", "status", "drivers", "constructors",
            "races", "results", "qualifying", "sprint_results",
            "pit_stops", "lap_times",
            "driver_standings", "constructor_standings", "constructor_results"
        };

        // 4. Bulk insert via Npgsql COPY
        foreach (var table in loadOrder)
        {
            if (mode == ImportMode.Full)
                await TruncateAndCopy(table, mapped[table]);
            else
                await UpsertCopy(table, mapped[table]);
        }

        // 5. Post-import validation
        await ValidateImport();
    }
}
```

#### Schema Mapping Details

Key transformations from Jolpica/Ergast → WeRace:

| Transformation | Source | Target | Logic |
|----------------|--------|--------|-------|
| Season FK | `races.year` (int) | `races.season_id` (FK) | Lookup `seasons.id WHERE year = races.year` |
| Table names | camelCase (`pitStops`) | snake_case (`pit_stops`) | String transformation |
| Column names | camelCase (`raceId`) | snake_case (`race_id`) | String transformation |
| Primary keys | Jolpica IDs | Preserved as-is | Keep original IDs for Jolpica compatibility |
| Sprint dates | Not in older dumps | `races.sprint_date/time` | Set NULL for pre-2021 races |
| Text encoding | MySQL latin1/utf8 | PostgreSQL UTF-8 | Ensure UTF-8 conversion |

#### Data Validation After Import

```sql
-- Post-import validation queries (run after every import)

-- 1. Row count sanity
SELECT 'seasons' AS tbl, COUNT(*) AS cnt FROM seasons
UNION ALL SELECT 'races', COUNT(*) FROM races
UNION ALL SELECT 'drivers', COUNT(*) FROM drivers
UNION ALL SELECT 'constructors', COUNT(*) FROM constructors
UNION ALL SELECT 'results', COUNT(*) FROM results
UNION ALL SELECT 'qualifying', COUNT(*) FROM qualifying
UNION ALL SELECT 'sprint_results', COUNT(*) FROM sprint_results
UNION ALL SELECT 'pit_stops', COUNT(*) FROM pit_stops
UNION ALL SELECT 'lap_times', COUNT(*) FROM lap_times
UNION ALL SELECT 'driver_standings', COUNT(*) FROM driver_standings
UNION ALL SELECT 'constructor_standings', COUNT(*) FROM constructor_standings;

-- 2. FK integrity (should return 0 orphans)
SELECT COUNT(*) AS orphan_results
FROM results r
LEFT JOIN races ra ON r.race_id = ra.id
WHERE ra.id IS NULL;

SELECT COUNT(*) AS orphan_standings
FROM driver_standings ds
LEFT JOIN races r ON ds.race_id = r.id
WHERE r.id IS NULL;

-- 3. Data range check
SELECT MIN(s.year) AS earliest_season, MAX(s.year) AS latest_season,
       COUNT(DISTINCT s.year) AS total_seasons
FROM seasons s;

-- 4. Spot check — known data points
SELECT r.name, d.surname, res.position
FROM results res
JOIN races r ON res.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN drivers d ON res.driver_id = d.id
WHERE s.year = 2023 AND r.round = 1 AND res.position = 1;
-- Expected: Bahrain GP, Verstappen, 1
```

---

## 4. API Contract (Phase 1 Endpoints)

### Base Configuration

```
Base URL:   /api/v1
Format:     JSON (application/json)
Versioning: URL path (/v1/)
Auth:       Bearer JWT token (for protected endpoints)
```

### Pagination Approach

**Recommendation for Q10:** Cursor-based pagination with `limit` + `offset` fallback.

- Default page size: 25
- Maximum page size: 100
- All list endpoints support `?limit=N&offset=N`
- Response includes `total` count and `hasMore` flag

```json
{
  "data": [...],
  "pagination": {
    "total": 857,
    "limit": 25,
    "offset": 0,
    "hasMore": true
  }
}
```

**Why offset, not cursor:** F1 data is static historical data. No real-time inserts that would cause cursor drift. Offset is simpler for the frontend to implement page navigation ("Show me page 5 of drivers"). Cursor-based would be premature complexity.

### Error Format

All errors follow a consistent envelope:

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Driver with id 99999 not found",
    "details": null
  }
}
```

Standard HTTP status codes:

| Code | Usage |
|------|-------|
| 200 | Success |
| 201 | Created (registration) |
| 400 | Bad request (invalid query params, validation error) |
| 401 | Unauthorized (no/invalid token — protected endpoints) |
| 403 | Forbidden (valid token, insufficient permissions) |
| 404 | Resource not found |
| 429 | Rate limited |
| 500 | Internal server error |

### OpenAPI / Swagger

```csharp
// src/WeRace.Api/Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "WeRace API",
            Version = "v1",
            Description = "Formula 1 historical data API"
        };
        return Task.CompletedTask;
    });
});

// ...

app.MapOpenApi();               // /openapi/v1.json
app.MapScalarApiReference();    // /scalar/v1 (interactive docs UI)
```

Scalar replaces Swagger UI — lighter, modern, better DX. Ships with .NET 10.

### Endpoint Definitions

#### Seasons (Public)

```
GET /api/v1/seasons
    Query: ?from={year}&to={year}&limit=25&offset=0
    Response 200:
    {
      "data": [
        { "id": 1, "year": 1950, "url": "/api/v1/seasons/1950" }
      ],
      "pagination": { "total": 76, "limit": 25, "offset": 0, "hasMore": true }
    }

GET /api/v1/seasons/{year}
    Response 200:
    {
      "id": 1,
      "year": 1950,
      "races": [
        {
          "id": 1,
          "round": 1,
          "name": "British Grand Prix",
          "circuit": { "id": 1, "name": "Silverstone Circuit", "country": "UK" },
          "date": "1950-05-13",
          "url": "/api/v1/races/1"
        }
      ],
      "wikipediaUrl": "https://en.wikipedia.org/wiki/1950_Formula_One_season"
    }

GET /api/v1/seasons/{year}/standings
    Response 200:
    {
      "year": 2023,
      "driverStandings": [
        {
          "position": 1,
          "driver": { "id": 830, "code": "VER", "forename": "Max", "surname": "Verstappen" },
          "constructor": { "id": 9, "name": "Red Bull" },
          "points": 575.0,
          "wins": 19
        }
      ],
      "constructorStandings": [
        {
          "position": 1,
          "constructor": { "id": 9, "name": "Red Bull" },
          "points": 860.0,
          "wins": 21
        }
      ]
    }
```

#### Races (Public)

```
GET /api/v1/races
    Query: ?season={year}&circuit_id={id}&limit=25&offset=0
    Response 200:
    {
      "data": [
        {
          "id": 1100,
          "season": 2023,
          "round": 1,
          "name": "Bahrain Grand Prix",
          "circuit": { "id": 3, "name": "Bahrain International Circuit", "country": "Bahrain" },
          "date": "2023-03-05",
          "url": "/api/v1/races/1100"
        }
      ],
      "pagination": { "total": 23, "limit": 25, "offset": 0, "hasMore": false }
    }

GET /api/v1/races/{id}
    Response 200:
    {
      "id": 1100,
      "season": 2023,
      "round": 1,
      "name": "Bahrain Grand Prix",
      "circuit": {
        "id": 3,
        "name": "Bahrain International Circuit",
        "location": "Sakhir",
        "country": "Bahrain",
        "latitude": 26.0325,
        "longitude": 50.5106
      },
      "date": "2023-03-05",
      "time": "15:00:00",
      "fp1Date": "2023-03-03", "fp1Time": "11:30:00",
      "fp2Date": "2023-03-03", "fp2Time": "15:00:00",
      "fp3Date": "2023-03-04", "fp3Time": "11:30:00",
      "qualiDate": "2023-03-04", "qualiTime": "15:00:00",
      "sprintDate": null, "sprintTime": null,
      "hasSprint": false,
      "wikipediaUrl": "https://en.wikipedia.org/wiki/2023_Bahrain_Grand_Prix"
    }

GET /api/v1/races/{id}/results
    Response 200:
    {
      "raceId": 1100,
      "raceName": "Bahrain Grand Prix",
      "results": [
        {
          "position": 1,
          "positionText": "1",
          "driver": { "id": 830, "code": "VER", "forename": "Max", "surname": "Verstappen" },
          "constructor": { "id": 9, "name": "Red Bull" },
          "grid": 1,
          "laps": 57,
          "time": "1:33:56.736",
          "points": 25.0,
          "fastestLap": 44,
          "fastestLapTime": "1:33.996",
          "status": "Finished"
        }
      ]
    }

GET /api/v1/races/{id}/qualifying
    Response 200:
    {
      "raceId": 1100,
      "raceName": "Bahrain Grand Prix",
      "qualifying": [
        {
          "position": 1,
          "driver": { "id": 830, "code": "VER", "forename": "Max", "surname": "Verstappen" },
          "constructor": { "id": 9, "name": "Red Bull" },
          "q1": "1:30.608",
          "q2": "1:29.773",
          "q3": "1:29.708"
        }
      ]
    }

GET /api/v1/races/{id}/sprint
    Response 200 (if sprint exists):
    {
      "raceId": 1100,
      "raceName": "Bahrain Grand Prix",
      "sprintResults": [...]
    }
    Response 404 (if no sprint for this race):
    { "error": { "code": "NOT_FOUND", "message": "No sprint race data for this event" } }

GET /api/v1/races/{id}/pit-stops
    Response 200:
    {
      "raceId": 1100,
      "pitStops": [
        {
          "driver": { "id": 830, "code": "VER", "surname": "Verstappen" },
          "stop": 1,
          "lap": 15,
          "time": "17:12:34",
          "duration": "23.640"
        }
      ]
    }

GET /api/v1/races/{id}/laps
    Query: ?driver_id={id}&limit=100&offset=0
    Response 200:
    {
      "raceId": 1100,
      "laps": [
        {
          "lap": 1,
          "driverId": 830,
          "position": 1,
          "time": "1:39.432",
          "milliseconds": 99432
        }
      ],
      "pagination": { "total": 57, "limit": 100, "offset": 0, "hasMore": false }
    }

GET /api/v1/races/{id}/standings
    Response 200:
    {
      "raceId": 1100,
      "raceName": "Bahrain Grand Prix",
      "afterRound": 1,
      "driverStandings": [...],
      "constructorStandings": [...]
    }
```

#### Drivers (Public)

```
GET /api/v1/drivers
    Query: ?nationality={string}&search={string}&limit=25&offset=0
    Response 200:
    {
      "data": [
        {
          "id": 830,
          "driverRef": "max_verstappen",
          "code": "VER",
          "forename": "Max",
          "surname": "Verstappen",
          "dateOfBirth": "1997-09-30",
          "nationality": "Dutch",
          "url": "/api/v1/drivers/830"
        }
      ],
      "pagination": { "total": 857, "limit": 25, "offset": 0, "hasMore": true }
    }

GET /api/v1/drivers/{id}
    Response 200:
    {
      "id": 830,
      "driverRef": "max_verstappen",
      "number": 1,
      "code": "VER",
      "forename": "Max",
      "surname": "Verstappen",
      "dateOfBirth": "1997-09-30",
      "nationality": "Dutch",
      "wikipediaUrl": "https://en.wikipedia.org/wiki/Max_Verstappen"
    }

GET /api/v1/drivers/{id}/career
    Response 200:
    {
      "driverId": 830,
      "name": "Max Verstappen",
      "seasons": 10,
      "races": 190,
      "wins": 54,
      "podiums": 98,
      "poles": 35,
      "fastestLaps": 28,
      "championships": 3,
      "points": 2586.5,
      "firstRace": { "id": 950, "name": "2015 Australian Grand Prix", "date": "2015-03-15" },
      "lastRace": { "id": 1122, "name": "2023 Abu Dhabi Grand Prix", "date": "2023-11-26" }
    }

GET /api/v1/drivers/{id}/results
    Query: ?season={year}&limit=25&offset=0
    Response 200:
    {
      "data": [
        {
          "race": { "id": 1100, "name": "Bahrain Grand Prix", "season": 2023, "date": "2023-03-05" },
          "position": 1,
          "grid": 1,
          "points": 25.0,
          "status": "Finished"
        }
      ],
      "pagination": { "total": 190, "limit": 25, "offset": 0, "hasMore": true }
    }
```

#### Constructors (Public)

```
GET /api/v1/constructors
    Query: ?nationality={string}&search={string}&limit=25&offset=0
    Response 200: (same pagination envelope as drivers)

GET /api/v1/constructors/{id}
    Response 200:
    {
      "id": 9,
      "constructorRef": "red_bull",
      "name": "Red Bull",
      "nationality": "Austrian",
      "wikipediaUrl": "https://en.wikipedia.org/wiki/Red_Bull_Racing"
    }

GET /api/v1/constructors/{id}/career
    Response 200:
    {
      "constructorId": 9,
      "name": "Red Bull",
      "seasons": 19,
      "races": 373,
      "wins": 113,
      "poles": 93,
      "championships": 6,
      "points": 7245.5,
      "firstRace": { "id": 530, "name": "2005 Australian Grand Prix" },
      "lastRace": { "id": 1122, "name": "2023 Abu Dhabi Grand Prix" }
    }

GET /api/v1/constructors/{id}/results
    Query: ?season={year}&limit=25&offset=0
    Response 200: (same shape as driver results, grouped by race)
```

#### Circuits (Public)

```
GET /api/v1/circuits
    Query: ?country={string}&search={string}&limit=25&offset=0
    Response 200: (standard pagination envelope)

GET /api/v1/circuits/{id}
    Response 200:
    {
      "id": 3,
      "circuitRef": "bahrain",
      "name": "Bahrain International Circuit",
      "location": "Sakhir",
      "country": "Bahrain",
      "latitude": 26.0325,
      "longitude": 50.5106,
      "altitude": 7,
      "wikipediaUrl": "https://en.wikipedia.org/wiki/Bahrain_International_Circuit"
    }

GET /api/v1/circuits/{id}/races
    Query: ?limit=25&offset=0
    Response 200:
    {
      "data": [
        {
          "id": 1100,
          "season": 2023,
          "round": 1,
          "name": "Bahrain Grand Prix",
          "date": "2023-03-05",
          "winnerId": 830,
          "winnerName": "Max Verstappen"
        }
      ],
      "pagination": { "total": 19, "limit": 25, "offset": 0, "hasMore": false }
    }
```

#### Auth Endpoints

```
POST /api/v1/auth/register
    Request:
    {
      "email": "user@example.com",
      "password": "SecureP@ss123"
    }
    Response 201:
    {
      "userId": "uuid",
      "email": "user@example.com"
    }

POST /api/v1/auth/login
    Request:
    {
      "email": "user@example.com",
      "password": "SecureP@ss123"
    }
    Response 200:
    {
      "accessToken": "eyJhbG...",
      "refreshToken": "dGhpcyBp...",
      "expiresIn": 3600,
      "tokenType": "Bearer"
    }

POST /api/v1/auth/refresh
    Request:
    {
      "refreshToken": "dGhpcyBp..."
    }
    Response 200: (same shape as login)

POST /api/v1/auth/logout
    Headers: Authorization: Bearer {token}
    Response 204

GET /api/v1/auth/me
    Headers: Authorization: Bearer {token}
    Response 200:
    {
      "userId": "uuid",
      "email": "user@example.com",
      "displayName": "Max"
    }
```

#### AI Namespace (Reserved — Phase 2)

```
POST /api/v1/ai/query              — NOT IMPLEMENTED (Phase 2)
GET  /api/v1/ai/conversations/{id}  — NOT IMPLEMENTED (Phase 2)

Response 501 for all /api/v1/ai/* endpoints in Phase 1:
{
  "error": {
    "code": "NOT_IMPLEMENTED",
    "message": "AI features are coming in the next update"
  }
}
```

### Endpoint Grouping in Code

```csharp
// src/WeRace.Api/Program.cs
var app = builder.Build();

app.MapGroup("/api/v1/seasons").MapSeasonEndpoints();
app.MapGroup("/api/v1/races").MapRaceEndpoints();
app.MapGroup("/api/v1/drivers").MapDriverEndpoints();
app.MapGroup("/api/v1/constructors").MapConstructorEndpoints();
app.MapGroup("/api/v1/circuits").MapCircuitEndpoints();
app.MapGroup("/api/v1/auth").MapAuthEndpoints();
app.MapGroup("/api/v1/ai").MapAiEndpoints();  // Returns 501 in Phase 1
```

```csharp
// src/WeRace.Api/Endpoints/SeasonEndpoints.cs
public static class SeasonEndpoints
{
    public static RouteGroupBuilder MapSeasonEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetSeasons).WithName("GetSeasons");
        group.MapGet("/{year:int}", GetSeasonByYear).WithName("GetSeasonByYear");
        group.MapGet("/{year:int}/standings", GetSeasonStandings).WithName("GetSeasonStandings");

        return group;
    }

    private static async Task<IResult> GetSeasons(
        [AsParameters] PaginationParams pagination,
        int? from, int? to,
        WeRaceDbContext db,
        CancellationToken ct)
    {
        var query = db.Seasons.AsQueryable();

        if (from.HasValue) query = query.Where(s => s.Year >= from.Value);
        if (to.HasValue) query = query.Where(s => s.Year <= to.Value);

        var total = await query.CountAsync(ct);
        var data = await query
            .OrderByDescending(s => s.Year)
            .Skip(pagination.Offset)
            .Take(pagination.Limit)
            .Select(s => new { s.Id, s.Year, Url = $"/api/v1/seasons/{s.Year}" })
            .ToListAsync(ct);

        return Results.Ok(new { Data = data, Pagination = new { Total = total, pagination.Limit, pagination.Offset, HasMore = pagination.Offset + data.Count < total } });
    }

    // ... other handlers
}
```

---

## 5. Authentication Infrastructure

### .NET Identity Setup

```csharp
// src/WeRace.Api/Program.cs — full auth configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // mobile keyboard UX
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // MVP — add email confirmation post-launch
})
.AddEntityFrameworkStores<WeRaceDbContext>();
```

### JWT Token Configuration

| Setting | Value | Rationale |
|---------|-------|-----------|
| Access token lifetime | 1 hour | Short-lived for security |
| Refresh token lifetime | 30 days | Mobile UX — users shouldn't re-login monthly |
| Refresh token rotation | Yes | New refresh token issued on each refresh (invalidates old) |
| Signing algorithm | HS256 | Sufficient for single-service auth; upgrade to RS256 if we federate |

```json
// appsettings.json
{
  "Jwt": {
    "Issuer": "werace-api",
    "Audience": "werace-app",
    "Key": "{{managed-by-aspire-user-secrets}}",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

### Registration and Login Flow

```
Registration:
1. Client POST /api/v1/auth/register { email, password }
2. Server validates (unique email, password policy)
3. Server creates IdentityUser + UserProfile row
4. Server returns 201 (no auto-login — client redirects to login)

Login:
1. Client POST /api/v1/auth/login { email, password }
2. Server validates credentials via Identity.SignInManager
3. Server generates JWT access token + refresh token
4. Server returns { accessToken, refreshToken, expiresIn }
5. Client stores tokens in secure storage (Keychain / Keystore)

Token Refresh:
1. Client POST /api/v1/auth/refresh { refreshToken }
2. Server validates refresh token (not expired, not revoked)
3. Server issues new access token + rotated refresh token
4. Old refresh token is invalidated

Logout:
1. Client POST /api/v1/auth/logout (with Bearer token)
2. Server revokes refresh token
3. Client deletes stored tokens
```

### Passkey / WebAuthn Integration

**Phase 1 approach:** Prepare the infrastructure but ship email/password first. Passkey endpoints are stubbed.

WebAuthn requires:

1. **FIDO2 server library:** `Fido2.AspNet` NuGet package
2. **Credential storage:** Additional table linking FIDO2 credentials to IdentityUser
3. **Relying Party ID:** `api.werace.app`

```sql
-- Passkey credential storage (Phase 1 — table created, endpoints stubbed)
CREATE TABLE passkey_credentials (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    identity_id         VARCHAR(450) NOT NULL,  -- FK to identity.asp_net_users.id
    credential_id       BYTEA NOT NULL UNIQUE,
    public_key          BYTEA NOT NULL,
    sign_count          BIGINT NOT NULL DEFAULT 0,
    transports          TEXT[],
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_used_at        TIMESTAMPTZ
);
```

**Why defer full passkey implementation:** React Native passkey support (iOS Passkeys API, Android Credential Manager) requires platform-specific native modules. The backend is ready; frontend integration is the unknown. Ship email/password, add passkeys when Dinesh confirms React Native feasibility (follow-up F4).

### Anonymous Access Middleware

No middleware needed. The approach is simpler:

- Public endpoints have **no** `[Authorize]` attribute — they serve anonymous requests by default.
- Protected endpoints use `.RequireAuthorization()` on the route group.

```csharp
// Public — no auth required
app.MapGroup("/api/v1/seasons").MapSeasonEndpoints();
app.MapGroup("/api/v1/races").MapRaceEndpoints();
app.MapGroup("/api/v1/drivers").MapDriverEndpoints();
app.MapGroup("/api/v1/constructors").MapConstructorEndpoints();
app.MapGroup("/api/v1/circuits").MapCircuitEndpoints();

// Auth endpoints (register/login are obviously public)
app.MapGroup("/api/v1/auth").MapAuthEndpoints();

// Protected — requires valid JWT
app.MapGroup("/api/v1/ai").MapAiEndpoints().RequireAuthorization();
```

---

## 6. AI Foundations (Zero-Cost Phase 2 Prep)

### `werace_ai_readonly` Role

```sql
-- db/roles.sql
-- AI agent database role — created in Phase 1, used in Phase 2
-- Defense-in-depth layer 1: database-level read-only enforcement

CREATE ROLE werace_ai_readonly WITH LOGIN PASSWORD '{{managed-secret}}';

-- Grant read-only on F1 data tables only (public schema)
GRANT USAGE ON SCHEMA public TO werace_ai_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO werace_ai_readonly;

-- Ensure future tables are also readable
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO werace_ai_readonly;

-- Explicitly deny access to identity schema
REVOKE ALL ON SCHEMA identity FROM werace_ai_readonly;

-- Execution limits for AI-generated queries
ALTER ROLE werace_ai_readonly SET statement_timeout = '5s';
ALTER ROLE werace_ai_readonly SET lock_timeout = '2s';

-- Connection limit (dedicated pool, not shared with app)
ALTER ROLE werace_ai_readonly CONNECTION LIMIT 5;
```

### Database Views for Common F1 Queries

```sql
-- db/views.sql
-- Pre-built views for common query patterns
-- Used by AI agent (Phase 2) and can back API endpoints

-- Race winners with full context
CREATE VIEW v_race_winners AS
SELECT
    s.year AS season,
    r.round,
    r.name AS race_name,
    r.date AS race_date,
    c.name AS circuit_name,
    c.country AS circuit_country,
    d.forename || ' ' || d.surname AS winner_name,
    d.code AS winner_code,
    d.nationality AS winner_nationality,
    con.name AS constructor_name,
    res.laps,
    res.time AS race_time,
    res.fastest_lap_time
FROM results res
JOIN races r ON res.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN circuits c ON r.circuit_id = c.id
JOIN drivers d ON res.driver_id = d.id
JOIN constructors con ON res.constructor_id = con.id
WHERE res.position = 1;

-- Driver career statistics
CREATE VIEW v_driver_career_stats AS
SELECT
    d.id AS driver_id,
    d.forename || ' ' || d.surname AS driver_name,
    d.code,
    d.nationality,
    COUNT(DISTINCT res.race_id) AS total_races,
    COUNT(*) FILTER (WHERE res.position = 1) AS wins,
    COUNT(*) FILTER (WHERE res.position <= 3) AS podiums,
    COUNT(*) FILTER (WHERE res.grid = 1) AS poles,
    COUNT(*) FILTER (WHERE res.rank = 1) AS fastest_laps,
    SUM(res.points) AS total_points,
    MIN(ra.date) AS first_race_date,
    MAX(ra.date) AS last_race_date,
    COUNT(DISTINCT s.year) AS seasons_active
FROM drivers d
JOIN results res ON d.id = res.driver_id
JOIN races ra ON res.race_id = ra.id
JOIN seasons s ON ra.season_id = s.id
GROUP BY d.id, d.forename, d.surname, d.code, d.nationality;

-- Constructor career statistics
CREATE VIEW v_constructor_career_stats AS
SELECT
    con.id AS constructor_id,
    con.name AS constructor_name,
    con.nationality,
    COUNT(DISTINCT res.race_id) AS total_races,
    COUNT(*) FILTER (WHERE res.position = 1) AS wins,
    COUNT(*) FILTER (WHERE res.position <= 3) AS podiums,
    COUNT(*) FILTER (WHERE res.grid = 1) AS poles,
    SUM(res.points) AS total_points,
    COUNT(DISTINCT s.year) AS seasons_active
FROM constructors con
JOIN results res ON con.id = res.constructor_id
JOIN races ra ON res.race_id = ra.id
JOIN seasons s ON ra.season_id = s.id
GROUP BY con.id, con.name, con.nationality;

-- Season final standings (last race of each season)
CREATE VIEW v_season_final_driver_standings AS
SELECT
    s.year AS season,
    ds.position,
    d.forename || ' ' || d.surname AS driver_name,
    d.code,
    ds.points,
    ds.wins
FROM driver_standings ds
JOIN races r ON ds.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN drivers d ON ds.driver_id = d.id
WHERE r.id = (
    SELECT r2.id FROM races r2
    WHERE r2.season_id = s.id
    ORDER BY r2.round DESC LIMIT 1
);

CREATE VIEW v_season_final_constructor_standings AS
SELECT
    s.year AS season,
    cs.position,
    con.name AS constructor_name,
    cs.points,
    cs.wins
FROM constructor_standings cs
JOIN races r ON cs.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN constructors con ON cs.constructor_id = con.id
WHERE r.id = (
    SELECT r2.id FROM races r2
    WHERE r2.season_id = s.id
    ORDER BY r2.round DESC LIMIT 1
);

-- Pit stop summary per race
CREATE VIEW v_pit_stop_summary AS
SELECT
    r.id AS race_id,
    r.name AS race_name,
    s.year AS season,
    d.forename || ' ' || d.surname AS driver_name,
    d.code,
    COUNT(*) AS total_stops,
    MIN(ps.milliseconds) AS fastest_stop_ms,
    AVG(ps.milliseconds) AS avg_stop_ms
FROM pit_stops ps
JOIN races r ON ps.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN drivers d ON ps.driver_id = d.id
GROUP BY r.id, r.name, s.year, d.id, d.forename, d.surname, d.code;

-- Head-to-head qualifying comparison (useful for AI "compare X vs Y" queries)
CREATE VIEW v_qualifying_head_to_head AS
SELECT
    q.race_id,
    r.name AS race_name,
    s.year AS season,
    q.driver_id,
    d.forename || ' ' || d.surname AS driver_name,
    d.code,
    q.position,
    q.q1, q.q2, q.q3,
    con.name AS constructor_name
FROM qualifying q
JOIN races r ON q.race_id = r.id
JOIN seasons s ON r.season_id = s.id
JOIN drivers d ON q.driver_id = d.id
JOIN constructors con ON q.constructor_id = con.id;
```

### Schema Documentation for AI System Prompt

The following metadata format will be injected into the AI agent's system prompt in Phase 2. Creating it now ensures the schema docs stay in sync with the actual schema.

```sql
-- db/schema_docs.sql
-- Machine-readable schema documentation for AI agent system prompt
-- Format: COMMENT ON TABLE/COLUMN for introspectable documentation

COMMENT ON TABLE seasons IS 'F1 seasons from 1950 to present. One row per year.';
COMMENT ON COLUMN seasons.year IS 'Calendar year (1950-present). Unique.';

COMMENT ON TABLE races IS 'Individual Grand Prix races within a season.';
COMMENT ON COLUMN races.round IS 'Race number within the season (1-based).';
COMMENT ON COLUMN races.sprint_date IS 'Sprint race date. NULL if no sprint format for this event.';

COMMENT ON TABLE drivers IS 'F1 drivers across all seasons.';
COMMENT ON COLUMN drivers.driver_ref IS 'URL-safe unique identifier, e.g. "max_verstappen".';
COMMENT ON COLUMN drivers.code IS 'Three-letter abbreviation shown on timing screens, e.g. "VER". NULL for pre-2000 drivers.';
COMMENT ON COLUMN drivers.number IS 'Permanent race number (post-2014). NULL for historical drivers.';

COMMENT ON TABLE constructors IS 'F1 teams/constructors across all seasons.';
COMMENT ON COLUMN constructors.constructor_ref IS 'URL-safe unique identifier, e.g. "red_bull".';

COMMENT ON TABLE results IS 'Race finishing results. One row per driver per race.';
COMMENT ON COLUMN results.position IS 'Finishing position. NULL if DNF/DNS/DSQ.';
COMMENT ON COLUMN results.position_text IS 'Human-readable position: "1", "2", "R" (retired), "D" (disqualified).';
COMMENT ON COLUMN results.grid IS 'Starting grid position.';
COMMENT ON COLUMN results.fastest_lap IS 'Lap number on which driver set their fastest lap. NULL if not recorded.';
COMMENT ON COLUMN results.rank IS 'Rank of this driver fastest lap vs all drivers in this race. 1 = fastest.';

COMMENT ON TABLE qualifying IS 'Qualifying session results. Separate from race results.';
COMMENT ON COLUMN qualifying.q1 IS 'Fastest Q1 lap time. NULL if did not participate in Q1.';
COMMENT ON COLUMN qualifying.q2 IS 'Fastest Q2 lap time. NULL if eliminated in Q1.';
COMMENT ON COLUMN qualifying.q3 IS 'Fastest Q3 lap time. NULL if eliminated in Q2.';

COMMENT ON TABLE sprint_results IS 'Sprint race results (2021+). Same structure as results but for sprint format.';

COMMENT ON TABLE pit_stops IS 'Pit stop events during a race. Available from 2012 onwards.';
COMMENT ON COLUMN pit_stops.stop IS 'Pit stop sequence number for this driver in this race (1st stop, 2nd stop, etc.).';
COMMENT ON COLUMN pit_stops.duration IS 'Pit stop duration as string, e.g. "23.640".';

COMMENT ON TABLE lap_times IS 'Individual lap times per driver per race. Coverage varies by era.';
COMMENT ON TABLE driver_standings IS 'Championship standings after each race. One row per driver per race.';
COMMENT ON TABLE constructor_standings IS 'Constructor championship standings after each race.';

COMMENT ON TABLE status IS 'Race finish status codes: "Finished", "Accident", "Engine", "+1 Lap", etc.';

COMMENT ON VIEW v_race_winners IS 'Denormalized view of race winners with season, circuit, driver, and constructor details.';
COMMENT ON VIEW v_driver_career_stats IS 'Aggregated career statistics per driver: wins, podiums, poles, points.';
COMMENT ON VIEW v_constructor_career_stats IS 'Aggregated career statistics per constructor.';
COMMENT ON VIEW v_season_final_driver_standings IS 'Final driver standings for each completed season.';
COMMENT ON VIEW v_season_final_constructor_standings IS 'Final constructor standings for each completed season.';
```

The AI system prompt will extract these comments via:

```sql
SELECT obj_description(oid) FROM pg_class WHERE relname = 'drivers';
SELECT col_description(attrelid, attnum), attname
FROM pg_attribute WHERE attrelid = 'drivers'::regclass AND attnum > 0;
```

This keeps schema documentation in the database itself — single source of truth, always in sync.

### Reserved AI Namespace

All `/api/v1/ai/*` routes are registered in Phase 1 but return `501 Not Implemented`:

```csharp
// src/WeRace.Api/Endpoints/AiEndpoints.cs
public static class AiEndpoints
{
    public static RouteGroupBuilder MapAiEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/query", () =>
            Results.Json(
                new { error = new { code = "NOT_IMPLEMENTED", message = "AI features are coming in the next update" } },
                statusCode: 501))
            .WithName("AiQuery")
            .WithSummary("Submit AI query (Phase 2)");

        group.MapGet("/conversations/{id}", (string id) =>
            Results.Json(
                new { error = new { code = "NOT_IMPLEMENTED", message = "AI features are coming in the next update" } },
                statusCode: 501))
            .WithName("GetConversation")
            .WithSummary("Get conversation history (Phase 2)");

        return group;
    }
}
```

---

## Appendix A: Aspire Resource Summary

| Resource | Type | Purpose | Phase |
|----------|------|---------|-------|
| `postgres` | PostgreSQL 17 | F1 data + Identity + user profiles | 1 |
| `werace` | PostgreSQL database | Single database, two schemas (`public`, `identity`) | 1 |
| `cache` | Redis 7 | Response caching, rate limit counters | 1 |
| `api` | .NET Project | WeRace.Api Minimal API | 1 |
| pgAdmin | Dev tool | Database browser (Aspire-managed) | 1 |
| RedisInsight | Dev tool | Cache inspector (Aspire-managed) | 1 |

## Appendix B: NuGet Dependencies

### WeRace.Api

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
<PackageReference Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="Aspire.StackExchange.Redis" />
<PackageReference Include="Scalar.AspNetCore" />
```

### WeRace.Infrastructure

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="EFCore.NamingConventions" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
```

### WeRace.Domain

```xml
<!-- No NuGet dependencies. POCO entities only. -->
```

### WeRace.DataImport

```xml
<PackageReference Include="Npgsql" />
<PackageReference Include="System.CommandLine" />
```

## Appendix C: Development Workflow

```bash
# Start everything
dotnet run --project src/WeRace.AppHost

# Run database migrations
dotnet ef database update --project src/WeRace.Infrastructure --startup-project src/WeRace.Api

# Seed data from Jolpica dump
dotnet run --project src/WeRace.DataImport -- --source db/seed/jolpica-dump.sql --mode full

# Run tests
dotnet test

# Generate OpenAPI spec
curl http://localhost:5000/openapi/v1.json > docs/openapi.json
```
