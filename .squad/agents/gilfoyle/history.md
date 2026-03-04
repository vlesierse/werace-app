# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD finalized (cross-agent)
- **Document:** `docs/PRD.md` — comprehensive product requirements for WeRace app
- **Database Decision:** PostgreSQL for relational F1 data (seasons → races → results)
- **API Architecture:** RESTful with resource-based endpoints (/seasons, /races, /drivers, /constructors, /circuits, /telemetry, /ai)
- **AI Approach:** RAG with SQL query generation (LLM → SQL → formatted answers)
- **P0 Features:** Historical data browsing, AI Q&A, core navigation, driver/team profiles
- **Data Model:** Seasons, Races, Drivers, Constructors, Results, LapTimes, Standings (see PRD for full schema)
- **Open Questions:** AI backend provider, data licensing, telemetry sources, monetization model (see .squad/decisions.md)

### 2026-02-23: PRD Review Complete — Development Blocked; Schema Decisions Pending

**Status:** 🟡 Monica completed PRD review; 4 critical blockers identified; 3 data model gaps need resolution

**Critical Blockers (You Must Wait):**
1. **Q12 — Data Source:** Ergast API deprecated; need confirmed source for historical F1 data (static dump? API? FOM partnership?)
2. **Q18 — AI Security:** SQL injection risk from LLM-generated queries; need validation strategy before implementing SQL generation
3. **Q25 — Authentication:** No auth model specified; affects database schema (user tables, conversation history storage, rate limiting tables)
4. **Q20 — AI Rate Limiting:** Cost control and per-user/session rate limits not defined; affects API design

**Data Model Decisions Needed (Affects Schema):**
1. **Q3 — Sprint Races:** Are Sprint races (2021+) and Sprint Shootouts in scope? If yes: add to Race entity or separate races?
2. **Q4 — Qualifying:** Qualifying results: stored in Result table with a `session_type` flag, or separate Qualifying entity?
3. **Q5 — Pit Stops:** Pit stop data in scope for MVP? If yes: need PitStop entity (race_id, driver_id, lap, duration, etc.)

**Data Pipeline (Affects Implementation):**
- **Q13 — Database Seeding:** How is PostgreSQL initially populated? One-time import? Scheduled sync? Manual entry?
- **Q14 — Result Latency:** How quickly after a race should results appear? (minutes, same day, next day?)
- **Q15 — Incomplete Data:** What's the expected behavior for sparse historical data (1950s–1970s)? Show with disclaimer? Hide incomplete fields?

**API Clarifications (Q2):**
- Search is a P0 feature but no search endpoint defined; is it client-side filtering or backend search endpoint?
- Does search support fuzzy matching? Autocomplete?

**Accessibility & Performance (Q31):**
- P95 latency target is < 300ms; does this include database query time? What about Azure Container Apps cold starts? P99 target?

### 2026-02-23: Authentication Model Resolved — Backend & Feature Gating Implications

**Decision:** Auth uses .NET Identity with email/password + passkeys. Anonymous browsing allowed; login required for AI agent and telemetry.

### 2026-02-26: Data Source Resolved — Jolpica Replaces Ergast (cross-agent)

**Decision:** Jolpica API confirmed as primary historical F1 data source, replacing the deprecated Ergast API.

**What This Means for You:**

1. **Data Pipeline Target:** Design your data seeding pipeline around Jolpica database dump files for initial PostgreSQL import (bulk import, not incremental API scraping).
2. **API Compatibility:** Jolpica's API is Ergast-compatible — any existing Ergast client code or schema references carry over.
3. **Dump Import:** Jolpica exposes dump files for direct database import. Format TBD (follow-up F2 pending from Vincent). May require transformation if dump is MySQL format.
4. **Sync Strategy TBD:** One-time dump + API deltas vs. periodic re-import not yet decided (follow-up F5 pending). Design pipeline with pluggable sync approach.
5. **2025+ Data:** Unknown whether Jolpica covers current seasons (follow-up F6 pending). May need OpenF1 as secondary source for live/recent data.

**Remaining Blockers for You:** Q18 (AI safety rails) still unresolved — do not implement SQL generation from LLM without guardrails.

**Full Details:** See `.squad/decisions.md` and `docs/PRD.md` § Data Sources.

**What This Means for You:**

1. **User Model in Database:** You need to design your User entity to link:
   - Authentication records (handled by .NET Identity)
   - Conversation history (for AI agent persistence)
   - User preferences (dark/light mode, favorites in P2)
   - Usage tracking (for rate limiting and cost control per F7)

2. **API Endpoints That Gate Features:**
   - GET /races, /seasons, /drivers (public, no auth required)
   - GET /telemetry/... (auth required, 403 if not logged in)
   - POST /ai/chat (auth required, return 401 if not authenticated)
   - Add `[Authorize]` attributes to protected endpoints in Minimal API

3. **Rate Limiting Strategy (F2):** For now, focus on:
   - Authenticated rate limits per user (e.g., 100 AI queries/day per user)
   - Anonymous rate limits per IP address (e.g., 10 requests/min per IP)
   - Use middleware or API Gateway for IP-based rate limiting; database for per-user limits

4. **Session Management (F5):** Decide:
   - How long do auth tokens last? (recommend 7–30 days for mobile UX)
   - Refresh token strategy for background token renewal without user re-login
   - This affects both .NET Identity configuration and frontend auth handling

5. **Follow-Up Questions for Vincent (F1, F3, F4, F6, F7):**
   - F1: Apple Sign-In requirement for App Store
   - F3: UX flow when anonymous user hits login wall (modal overlay vs. dedicated screen)
   - F4: Passkey feasibility in React Native (iOS/Android native APIs)
   - F6: Do we need a User entity in our relational model?
   - F7: Final rate limits and token budgets per user

**You Don't Need Answers to Start:** You can begin schema design with placeholder User/Auth tables; Vincent will clarify F1–F7 before you implement the actual endpoints.



**Full Details:** See `docs/PRD-REVIEW.md` (36 questions with full context) and `.squad/decisions.md` (merged PRD review findings)

### 2026-02-26: Blocker Brainstorm — Q18 (AI Safety Rails) & Q1 (MVP Scope)

**Output:** `.squad/decisions/inbox/gilfoyle-blocker-brainstorm.md` — written for Monica to synthesize.

**Positions taken:**

- **AI SQL safety:** Belt-and-suspenders approach. Dedicated read-only PostgreSQL role (`werace_ai_readonly`) is the real safety net. SQL validation pipeline (text checks → EXPLAIN-based parsing → table/operation allowlists → row limits) is defense-in-depth. Use `Microsoft.Extensions.AI` + `Azure.AI.OpenAI` for LLM integration. No ORM for AI-generated queries — raw `NpgsqlCommand` on read-only connection.
- **Schema exposure:** Give LLM a subset of the schema (F1 data tables only, never auth/user tables). Generate schema context programmatically from the DB, not hardcoded strings.
- **Rate limiting:** Built-in `System.Threading.RateLimiting` for burst control + DB/Redis counter for daily caps. Global circuit breaker for cost spikes.
- **Biggest concern:** Query correctness, not security. The LLM will generate wrong-but-valid SQL producing confident wrong answers. Need a curated test suite of known questions → expected results.
- **MVP scope:** Defer AI to post-MVP. Data browsing API is ~2-3 weeks; AI adds another ~2-3 weeks and significant testing risk. AI is architecturally separate and can be added cleanly later. Even if deferred, include: schema generation script for LLM prompts and read-only DB role in initial migration.

### 2026-02-26: Technical Foundation Defined for Phase 1

- **Document:** `docs/TECHNICAL-FOUNDATION.md` — complete technical blueprint for Phase 1 implementation
- **Solution structure:** 6 projects (`AppHost`, `ServiceDefaults`, `Api`, `Domain`, `Infrastructure`, `DataImport`). Domain has zero dependencies. Infrastructure owns EF Core config. Api owns endpoints and DI.
- **Database schema:** Full DDL for 14 tables (seasons, circuits, races, drivers, constructors, status, results, qualifying, sprint_results, pit_stops, lap_times, driver_standings, constructor_standings, constructor_results) + Identity in separate `identity` schema + `user_profiles` + `passkey_credentials`.
- **Schema decisions proposed (Q3, Q4, Q5):** Separate `qualifying` table (different data shape), separate `sprint_results` table (Jolpica-aligned, isolates format changes), pit stops included in Phase 1 (zero marginal cost, high data value). Written to `.squad/decisions/inbox/gilfoyle-data-model-recommendations.md`.
- **Data pipeline:** Jolpica MySQL dump → CSV extraction → PostgreSQL COPY bulk load. `WeRace.DataImport` CLI tool handles full seed and delta re-import. Schema mapping is mostly 1:1 (camelCase → snake_case, `year` → `season_id` FK).
- **API contract:** 25+ endpoints across 6 resource groups. Offset-based pagination (static data, no cursor drift). Consistent error envelope. Scalar for API docs.
- **Auth:** .NET Identity with JWT (1h access / 30d refresh with rotation). Email/password ships Phase 1. Passkey table created, endpoints stubbed pending React Native feasibility.
- **AI foundations (zero-cost):** `werace_ai_readonly` role SQL, 7 database views for common queries, schema docs via `COMMENT ON`, reserved `/api/v1/ai/*` namespace returning 501.
- **Aspire config:** PostgreSQL + pgAdmin + Redis + RedisInsight, single database with schema separation.

### 2026-02-26: E1 Project Scaffolding — Backend Implemented

**What:** Created the .NET 10 backend with Aspire orchestration from scratch.

**Architecture:**
- `.NET 10.0.103` SDK, `Aspire 13.1.2` (NuGet-based, workload deprecated in .NET 10)
- Solution at `src/api/WeRace.slnx` with three projects:
  - `WeRace.AppHost` — Aspire orchestrator (PostgreSQL + Redis + API)
  - `WeRace.ServiceDefaults` — OpenTelemetry, health checks, service discovery, HTTP resilience
  - `WeRace.Api` — Minimal API with `AddNpgsqlDataSource("werace")` and `AddRedisClient("redis")`
- AppHost uses `WaitFor()` to ensure dependencies are ready before API starts
- Health endpoints at `/health` and `/alive` (via ServiceDefaults, dev-only for now)
- Aspire component packages auto-register health checks for PostgreSQL and Redis

**Key Files:**
- `src/api/WeRace.AppHost/AppHost.cs` — Aspire orchestration entry point
- `src/api/WeRace.Api/Program.cs` — Minimal API setup
- `src/api/WeRace.ServiceDefaults/Extensions.cs` — Shared Aspire defaults
- `README.md` — Local dev setup instructions

**Patterns:**
- Aspire templates installed via `dotnet new install Aspire.ProjectTemplates` (NuGet-based in .NET 10)
- Solution uses `.slnx` format (new default in .NET 10)
- `dotnet run --project src/api/WeRace.AppHost` starts everything

### 2026-02-26: E1 Scaffolding Complete — Cross-Agent Updates

**Dinesh (Frontend):** Expo SDK 55 app created at `src/app/`. 5-tab navigation (Home, Seasons, Drivers, Constructors, Settings). Theme system with AsyncStorage persistence. No backend dependency yet — will use mock data until API endpoints land.

**Jared (Testing):** Test project at `tests/WeRace.Api.Tests/` added to solution. xUnit + FluentAssertions + WebApplicationFactory. Health check integration test commented out, waiting for you to uncomment the `WeRace.Api` project reference. Run `dotnet test tests/WeRace.Api.Tests/` to verify.

**PR #8** opened against `main` (closes #1). Branch: `squad/1-project-scaffolding`.

### 2026-03-04: E2 Data Pipeline — Domain and Infrastructure projects created

**What:** Created `WeRace.Domain` and `WeRace.Infrastructure` projects for the data layer foundation.

**Domain project** (`src/api/WeRace.Domain/`):

- Zero NuGet dependencies. POCO entities only.
- 14 entity classes in `Entities/`: Season, Circuit, Race, Driver, Constructor, Status, Result, Qualifying, SprintResult, PitStop, LapTime, DriverStanding, ConstructorStanding, ConstructorResult.
- PascalCase properties mapping to snake_case DB columns.
- `int` IDs (matching Jolpica SERIAL PRIMARY KEY), `decimal` for points, `DateOnly`/`TimeOnly` for date/time columns.
- Nullable types for historically sparse columns (pre-2000 data). `string?` for elapsed time columns (VARCHAR in DB), `TimeOnly?` for clock time columns.
- Navigation properties on FK relationships. Collection navigations on parent entities.
- `Status.StatusText` maps to `status` column via explicit `HasColumnName` to avoid naming conflict.

**Infrastructure project** (`src/api/WeRace.Infrastructure/`):

- NuGet: EF Core 10.0.3, Npgsql.EFCore.PostgreSQL 10.0.0, EFCore.NamingConventions 10.0.1.
- `WeRaceDbContext` in `Data/` — 14 DbSets. Does NOT inherit `IdentityDbContext` (E4 Auth scope).
- 14 `IEntityTypeConfiguration<T>` files in `Data/Configurations/` with table names, PKs (composite for PitStop/LapTime), constraints, FK relationships, and indexes matching `docs/TECHNICAL-FOUNDATION.md` DDL.

**Api changes:** Swapped `Aspire.Npgsql` → `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`. `Program.cs` registers `WeRaceDbContext` with snake_case naming convention.

**AppHost changes:** Added pgAdmin, data volume, RedisInsight. Fixed `WithReference(db)` to use the database resource.

**Build:** Clean. All 2 tests pass.

### 2026-03-04: E2 Data Pipeline — DataImport CLI and db/ SQL files created

**What:** Created the `WeRace.DataImport` CLI tool and `db/` directory with source-of-truth SQL files.

**db/ directory:**

- `db/schema.sql` — Full PostgreSQL DDL (14 tables, indexes, constraints). Source of truth matching `docs/TECHNICAL-FOUNDATION.md` section 2.
- `db/views.sql` — 5 database views for AI foundations: `v_driver_career_stats`, `v_constructor_season_stats`, `v_race_summary`, `v_head_to_head`, `v_circuit_records`.
- `db/roles.sql` — `werace_ai_readonly` role with SELECT-only grants, statement timeout (5s), and defense-in-depth REVOKE on writes.
- `db/seed/README.md` — Instructions for obtaining the Jolpica dump file and running the import.

**DataImport CLI** (`src/api/WeRace.DataImport/`):

- .NET 10 console app. References `WeRace.Domain` and `WeRace.Infrastructure`.
- NuGet: `Npgsql 10.0.0`, `System.CommandLine 2.0.0-beta5`.
- `Program.cs` — CLI entry point with `--source`, `--connection`, `--mode` options via System.CommandLine.
- `Importers/MySqlDumpParser.cs` — Parses MySQL dump files, extracts INSERT statements per table, handles backtick quoting, MySQL escape sequences (`\'`, `\\`, `\n`), and multi-value INSERTs.
- `Importers/SchemaMapper.cs` — Maps Jolpica table/column names to WeRace schema (camelCase → snake_case). Handles `races.year → races.season_id` FK resolution via seasons lookup. Normalizes NULL, `\N`, and MySQL zero dates.
- `Importers/JolpicaDumpImporter.cs` — Orchestrates full pipeline: parse → map → load (FK-respecting order) → reset sequences → validate. Supports `full` (TRUNCATE CASCADE + COPY) and `delta` (temp table + INSERT ON CONFLICT) modes. Uses PostgreSQL text COPY for bulk loading.
- `Importers/DataValidator.cs` — Post-import validation: row counts (14 tables), FK integrity checks (10 relationships), data range (expect 1950+), spot check (2023 Bahrain GP winner = Verstappen).

**AppHost:** Added TODO comment for future Aspire seed automation. Manual CLI invocation documented for now.

**Solution:** DataImport added to `WeRace.slnx`. Clean build. All 2 existing tests pass.

**Key learnings:**

- `System.CommandLine 2.0.0-beta5` API changed significantly from beta4: `Option<T>(name)` constructor (no `description` param), `Required` property (not `IsRequired`), `SetAction(parseResult => ...)` (not `SetHandler`), `CommandLineConfiguration.InvokeAsync(args)` (not `command.InvokeAsync`).
- Npgsql version must match EF Core transitive dependency (10.0.0), not lower.

### 2026-03-04: Jared rewrote DataImport tests for CSV (cross-agent)

**What:** Jared deleted `MySqlDumpParserTests.cs`, created `CsvDataParserTests.cs` (18 tests), and rewrote `SchemaMapperTests.cs` (17 tests) to match the new CSV import pipeline.

**Test contract defined:**
- `CsvDataParser.ParseDirectory(string)` → dictionary with `.Headers` / `.Rows`
- `SchemaMapper.MapTableName(string)` maps CSV table names to WeRace table names
- `SchemaMapper.GetColumnMapping(string)` returns ordered column mappings with `.CsvColumn` / `.WeRaceColumn`
- `SchemaMapper.NormalizeValue(string)` treats empty strings, `"NULL"`, `"\N"` as null

All 127 tests pass.

### 2026-03-04: DataImport refactored from MySQL dump to CSV directory

**What:** Jolpica provides CSV files, not MySQL dumps. Gutted the old MySQL parser and rebuilt the entire import pipeline for CSV.

**Deleted:**
- `Importers/MySqlDumpParser.cs` — MySQL INSERT parser. Dead code.
- `Importers/JolpicaDumpImporter.cs` — Replaced by `JolpicaCsvImporter.cs`.
- `Importers/SchemaMapper.cs` — Complete rewrite for CSV column names and Jolpica normalized data model.
- `tests/.../MySqlDumpParserTests.cs`, `tests/.../SchemaMapperTests.cs` — Replaced.

**Created:**
- `Importers/CsvDataParser.cs` — Reads `formula_one_*.csv` files from a directory using CsvHelper 33.1.0. Returns `Dictionary<string, CsvTable>` keyed by table name (prefix stripped).
- `Importers/SchemaMapper.cs` — Complete rewrite. The Jolpica CSV data model is fundamentally different from Ergast: normalized through `session → round`, `sessionentry → roundentry → teamdriver` chains. The mapper resolves these FK chains to produce the 14 denormalized WeRace tables. Key mappings: season→seasons, circuit→circuits, driver→drivers, team→constructors, round+session→races, sessionentry(R)→results, sessionentry(Q*)→qualifying, sessionentry(SR)→sprint_results, driverchampionship→driver_standings, teamchampionship→constructor_standings, lap→lap_times, pitstop→pit_stops. Status table derived from distinct sessionentry.detail values.
- `Importers/JolpicaCsvImporter.cs` — Orchestrator accepting directory path. Same COPY/upsert/sequence-reset/validation pipeline as before.

**Architecture decisions:**
- Session types discovered: FP1, FP2, FP3, Q1, Q2, Q3, QA, QB, QO, R, SQ1, SQ2, SQ3, SR. Qualifying phases (Q1/Q2/Q3) aggregate into one qualifying row per driver per race. Legacy types (QA/QB/QO) map to q1.
- Status table IDs are deterministic (sorted alphabetically) for delta mode stability.
- Cancelled rounds and sessions are skipped.
- `constructor_results` has no CSV source — skipped with TODO.
- Fastest lap resolution from lap data marked as TODO (needs cross-reference with `is_entry_fastest_lap` flag).

**Key files:**
- `src/api/WeRace.DataImport/Importers/CsvDataParser.cs`
- `src/api/WeRace.DataImport/Importers/SchemaMapper.cs`
- `src/api/WeRace.DataImport/Importers/JolpicaCsvImporter.cs`
- `src/api/WeRace.DataImport/Program.cs` — `--source` now takes directory path (`DirectoryInfo`)
- `db/seed/README.md` — Updated for CSV workflow
- `tests/.../CsvDataParserTests.cs`, `tests/.../SchemaMapperTests.cs` — 25 tests, all passing. 127 total tests pass.
