# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD finalized (cross-agent)
- **Document:** `docs/PRD.md` — comprehensive product requirements for WeRace app
- **P0 (MVP) Features:** Historical data browsing, AI Q&A, core navigation with dark/light mode, driver/team profiles
- **P1 (Post-MVP) Features:** Race weekend companion, telemetry exploration, enhanced AI agent
- **P2 (Future) Features:** Personalization, circuit explorer, offline mode
- **User Personas:** Historian (casual fan, trivia), Analyst (data enthusiast, telemetry), Weekend Warrior (race companion)
- **Data Model:** Seasons, Races, Drivers, Constructors, Results, LapTimes, Standings
- **API:** RESTful resource-based endpoints — plan test coverage for each endpoint
- **Open Questions:** AI backend, data licensing, telemetry sources, monetization (see .squad/decisions.md)

### 2026-02-23: PRD Review Complete — Acceptance Criteria Pending

**Status:** 🟡 PRD review revealed gaps in acceptance criteria; cannot write test cases until blockers resolved

**Critical Issue:** PRD lacks specific acceptance criteria for most features. You need them for test planning.

**Examples of Vague Requirements (See `.squad/decisions.md` for full list):**
- "Smooth scrolling" → need frame rate target (60fps? jank budget?)
- "Instant feedback" → need millisecond threshold (< 100ms for UI feedback?)
- "Concise answers" (AI agent) → max word count? 1 sentence? 1 paragraph?
- "Comprehensive database" → which seasons have full coverage? Minimum data per race?

**Blocked on These Questions (Can't Write Test Cases):**
1. **Q1 — MVP Scope:** Which features are actually P0? 3 major features = high risk; recommend phasing
2. **Q7 — Empty States:** What do screens show when no data? No internet? Off-season Home?
3. **Q18 — AI Validation:** How do you test AI agent accuracy without knowing confidence scoring threshold?
4. **Q25 — Authentication:** Can't test conversation persistence or rate limiting without knowing if app has user accounts
5. **Q26 — Platform Support:** iOS/Android minimum versions affect test device matrix

**Data Model Gaps (Affects Test Coverage):**
- Qualifying results: how are they stored/displayed?
- Sprint races: are they in scope?
- Pit stops: are they in scope?

**Performance Testing (Q31):**
- P95 latency < 300ms for data queries; need to know:
  - Does this include cold starts?
  - Is there a P99 target?
  - What's the concurrent user load we're testing for? (Q30 unanswered)

**You Can Start:**
- Planning test structure and test categories (unit, integration, E2E, performance, accessibility)
- Gathering test requirements from persona use cases
- Setting up test environment and automation framework
- Identifying edge cases that will need acceptance criteria

**Full PRD Review:** See `docs/PRD-REVIEW.md` (36 questions with reasoning) and `.squad/decisions.md` (merged findings)

### 2026-02-26: All 4 Critical Blockers Resolved — Development Unblocked (cross-agent)

**Status:** ✅ All critical blockers resolved. Phase 1 sprint planning complete. Sprint 1 starts now.

**Resolved Blockers:**
- Q25 ✅ Authentication: .NET Identity, email/password + passkeys, anonymous browsing
- Q12 ✅ Data Source: Jolpica replaces Ergast, database dump import
- Q18 ✅ AI Safety: Defense-in-depth (4-layer stack), 50/day, historical-only — implemented in Phase 2
- Q1 ✅ MVP Scope: Phase 1 (5 weeks) data + auth, Phase 2 (2-3 weeks) AI

### 2026-02-26: Phase 1 Plan — Your Assignments

**Plan:** `docs/PHASE1-PLAN.md` (5 sprints × 1 week, 6 epics)
**Technical Foundation:** `docs/TECHNICAL-FOUNDATION.md` (solution structure, DDL, API contracts)

**Your Sprint-by-Sprint Work:**

| Sprint | Tasks | Epic | Days |
|--------|-------|------|------|
| S1 | Test infrastructure setup (xUnit, Jest) | E6 | 2 |
| S2 | API endpoint unit tests (seasons, races), data integrity validation tests | E6 | 3 |
| S3 | API tests (circuits, standings, qualifying), integration tests (pipeline import) | E6 | 3 |
| S4 | Auth flow tests (register, login, gated access), E2E test skeleton (3 core flows) | E6 | 4 |
| S5 | Performance baseline tests (P95 latency), full regression test pass | E6 | 3 |

**Key Decisions That Affect You:**
1. **Test coverage target:** >80% unit test coverage for API endpoints.
2. **E2E flows to cover:** (a) Browse season → race → results, (b) search driver, (c) register + login.
3. **Performance baseline:** P95 < 300ms for all data endpoints (warm cache). Automated check for top 10 endpoints.
4. **Integration tests:** Data pipeline import → query → verify. Auth flows end-to-end.
5. **Quality gate:** Zero critical/high-severity bugs open at Phase 1 ship.
6. **API contracts:** All endpoints use consistent JSON envelope `{ "data", "pagination", "error" }`. Test response shapes against `docs/API-CONTRACTS.md`.

**Acceptance Criteria You Verify:**
- All Jolpica data loads without errors (seasons 1950–present)
- Foreign key constraints pass (no orphaned results)
- All API endpoints return correct data validated against source
- Pagination works correctly
- Redis cache hit ratio >80% for repeated requests
- All database views return correct aggregations
- `werace_ai_readonly` role is SELECT-only (verified)

**Dependencies:**
- Test infrastructure can start immediately (S1, no blockers)
- API tests follow Gilfoyle's endpoint delivery schedule
- E2E tests need both API and frontend screens (S4)

**Data Model Decisions (Q3, Q4, Q5) — Know This:**
- Q3: Sprint races → separate `sprint_results` table (Jolpica-aligned)
- Q4: Qualifying → separate `qualifying` table with Q1/Q2/Q3 columns
- Q5: Pit stops → included in Phase 1 (`pit_stops` table, `GET /races/{id}/pit-stops`)

### 2026-02-26: Sprint 1 — Test Infrastructure Scaffolding Complete

**Status:** ✅ Test infrastructure created and verified. Placeholder tests pass.

**Backend test project:** `tests/WeRace.Api.Tests/`
- xUnit + FluentAssertions + coverlet + Microsoft.AspNetCore.Mvc.Testing
- Targeting net10.0 (matches .NET SDK 10.0.103)
- `HealthCheckTests.cs` — placeholder passes, real health check test commented out (waiting for API project)
- `WeRace.Api` project reference commented out — Gilfoyle needs to uncomment when API project exists
- Build: ✅ 0 warnings, 0 errors. Tests: ✅ 1/1 passed.

**Frontend test file:** `src/app/__tests__/App.test.tsx`
- Jest placeholder test (Expo comes with Jest pre-configured)
- Real App render test commented out — waiting for Dinesh to create the App component

**Documentation:** `docs/TESTING.md`
- Full test strategy: pyramid, commands, coverage targets (≥80% floor), naming conventions
- Test data strategy: real F1 data fixtures, TestContainers for integration tests

**Key file paths:**
- `tests/WeRace.Api.Tests/WeRace.Api.Tests.csproj` — backend test project
- `tests/WeRace.Api.Tests/HealthCheckTests.cs` — health check integration test (scaffolded)
- `tests/WeRace.Api.Tests/GlobalUsings.cs` — shared usings (xUnit + FluentAssertions)
- `src/app/__tests__/App.test.tsx` — frontend render test (scaffolded)
- `docs/TESTING.md` — test strategy document

**Decision:** Test projects at `tests/` not `src/api/` — follows TECHNICAL-FOUNDATION.md solution structure.
**Decision:** Gilfoyle must add test project to solution: `dotnet sln add tests/WeRace.Api.Tests/`

### 2026-02-26: E1 Scaffolding Complete — Cross-Agent Updates

**Gilfoyle (Backend):** Backend created at `src/api/` with 3 projects (AppHost, ServiceDefaults, Api). Health endpoints at `/health` and `/alive`. Test project reference to `WeRace.Api` now active — health check integration test ready to uncomment. Solution builds 0 warnings, 0 errors.

**Dinesh (Frontend):** Expo app at `src/app/` with 5 screens. Jest placeholder passes. Real render test in `__tests__/App.test.tsx` ready to uncomment.

**Post-work:** Test project wired into solution. 1/1 tests pass. PR #8 opened against `main` (closes #1). Branch: `squad/1-project-scaffolding`.

### 2026-03-04: Data Pipeline Tests Written (Issue #2)

**What:** Wrote comprehensive tests for the E2 data pipeline covering all four layers: Domain entities, Infrastructure DbContext, DataImport parser, and schema mapper.

**Test files created (4 files, 173 total tests):**

- `tests/WeRace.Api.Tests/DataImport/MySqlDumpParserTests.cs` — 15 tests: INSERT parsing, backtick-quoted names, escape handling (backslash, double-quote), NULL values, numerics, multi-table dumps, non-INSERT skipping, empty files, commas/parentheses in strings
- `tests/WeRace.Api.Tests/DataImport/SchemaMapperTests.cs` — 30 tests: camelCase-to-snake_case table mapping, column name conversion, FK resolution (races.year → season_id), pass-through tables, NormalizeValue handling

### 2026-03-04: DataImport Tests Rewritten for CSV Migration

**Status:** ✅ Test files rewritten. Blocked on Gilfoyle's implementation code (won't compile until `CsvDataParser.cs` and updated `SchemaMapper.cs` land).

**What changed:**
- **DELETED** `MySqlDumpParserTests.cs` (MySQL dump parser being removed)
- **CREATED** `CsvDataParserTests.cs` — 18 tests covering: table name extraction (strip `formula_one_` prefix), header parsing, data rows, empty fields, quoted fields (commas, newlines, escaped quotes), header-only CSVs, empty directories, missing directories, non-CSV file filtering, real-world format tests for season/circuit/driver/round/team CSVs
- **REWRITTEN** `SchemaMapperTests.cs` — 17 tests covering: CSV table name mapping (season→seasons, circuit→circuits, driver→drivers, round→races, team→constructors), case-insensitive mapping, column mapping for all 5 core tables (CSV column names → WeRace DB column names), unknown table handling, NormalizeValue (empty string→null, NULL→null, \\N→null, regular values pass through)

**Expected API surface for Gilfoyle:**
- `CsvDataParser.ParseDirectory(string directoryPath)` → `Dictionary<string, T>` where T has `.Headers` (string[]) and `.Rows` (List<string[]>)
- `SchemaMapper.MapTableName(string csvTable)` — maps CSV table names (without formula_one_ prefix) to WeRace table names
- `SchemaMapper.GetColumnMapping(string csvTable)` → array of objects with `.CsvColumn` and `.WeRaceColumn` properties
- `SchemaMapper.NormalizeValue(string value)` — kept, updated to treat empty strings as null

**Build errors (31 total):** All expected — `CsvDataParser` class doesn't exist yet (16), `GetColumnMapping` method doesn't exist yet (11), old methods removed (4). Zero syntax errors in test code.

### 2026-03-04: Gilfoyle completed CSV refactor — tests green (cross-agent)

**What:** Gilfoyle implemented the CSV import pipeline matching Jared's test contract. All build errors resolved. 127 tests pass.

**Key changes by Gilfoyle:**
- Deleted `MySqlDumpParser.cs`, created `CsvDataParser.cs` with CsvHelper 33.1.0
- Rewrote `SchemaMapper.cs` for Jolpica normalized FK chains (sessionentry → roundentry → teamdriver)
- Created `JolpicaCsvImporter.cs` replacing `JolpicaDumpImporter.cs`
- `Program.cs` `--source` now takes `DirectoryInfo` instead of `FileInfo`
- Status table derived at import time from distinct `sessionentry.detail` values
- Session routing: R → results, Q* → qualifying, SR → sprint_results, FP/SQ → skipped
- `tests/WeRace.Api.Tests/Domain/EntityTests.cs` — 30 tests: property existence, types, nullability (nullable vs non-nullable), navigation properties, collection initialization for Season, Race, Driver, Result, Constructor, Circuit, Status, PitStop, LapTime, Qualifying, SprintResult, DriverStanding, ConstructorStanding, ConstructorResult
- `tests/WeRace.Api.Tests/Infrastructure/DbContextTests.cs` — 96 tests: 14 DbSets registered, snake_case table names, composite PKs (PitStop, LapTime), single-column PKs, FK relationships (23 foreign keys verified), index coverage (15 indexed columns), unique indexes (year, driver_ref, circuit_ref, season_id+round), all column names match snake_case pattern

**Bug found and fixed:** `MySqlDumpParser.ProcessInsertStatement` had an off-by-one in its `VALUES` keyword position calculation. The `\s*` after `VALUES` in the regex shifted the match length, causing `IndexOf("VALUES", match.Index + match.Length - 6)` to overshoot by one position. Fixed by searching from after the table name capture group instead.

**Project references added:** Test project now references `WeRace.Domain`, `WeRace.Infrastructure`, and `WeRace.DataImport`.
