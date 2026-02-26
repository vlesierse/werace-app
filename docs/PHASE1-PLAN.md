# Phase 1 Plan — WeRace

**Version:** 1.0
**Created:** 2026-02-26
**Author:** Richard (Lead / Architect)
**Status:** Ready for execution
**Timeline:** 5 weeks (5 × 1-week sprints)
**Team:** Richard, Gilfoyle, Dinesh, Jared, Monica

---

## 1. Phase 1 Scope Definition

### What Ships in Phase 1

Phase 1 delivers a fully functional F1 historical data browsing app with authentication:

1. **Project infrastructure** — Aspire orchestration, .NET 10 Minimal API, React Native app skeleton, PostgreSQL database, Redis cache, CI pipeline
2. **Data pipeline** — Jolpica database dump import into PostgreSQL, seeded with full historical data (1950–present)
3. **REST API** — Public endpoints for seasons, races, results, drivers, constructors, circuits, standings, qualifying, laps
4. **Authentication** — .NET Identity with email/password and passkey support; anonymous browsing for public data; authenticated gate for future AI/telemetry endpoints
5. **Mobile UI** — React Native Paper app with dark/light mode, bottom tab navigation, all browse/detail screens, search
6. **AI foundations** — Zero-cost preparation for Phase 2 (no AI features exposed to users)

### What Is Explicitly OUT of Scope

| Feature | Deferred To | Reason |
|---------|-------------|--------|
| AI Q&A agent (all `/ai/*` endpoints) | Phase 2 | Reduces unknowns on critical path |
| Telemetry data exploration | Phase 2+ | Requires OpenF1/FastF1 pipeline |
| Race weekend companion (live/schedule) | Phase 2+ | Requires live data feed |
| Push notifications | Phase 2+ | Not needed without live features |
| Personalization (favorites, bookmarks) | Phase 2+ | Requires AI/engagement baseline |
| Offline mode | Phase 3 | P2 feature per PRD |
| Circuit explorer (3D maps) | Phase 3 | P2 feature per PRD |
| Analytics SDK integration | Phase 2 | Nice-to-have but not launch-blocking for internal release |
| "AI Coming Soon" teaser | TBD in sprint | Vincent deferred to team; decide during Sprint 3 |

### AI Foundations Included in Phase 1

These are zero-cost investments that accelerate Phase 2 with no user-facing surface:

| Foundation | Owner | Sprint | Purpose |
|------------|-------|--------|---------|
| `werace_ai_readonly` PostgreSQL role | Gilfoyle | S2 | Read-only database user for AI SQL execution |
| Database views for common queries | Gilfoyle | S3 | Pre-built views: driver career stats, constructor season stats, race summaries, head-to-head records |
| Schema documentation (`docs/SCHEMA.md`) | Richard | S3 | Column descriptions, relationships, enums — feeds LLM system prompts |
| Reserve `/ai` namespace in API routing | Gilfoyle | S2 | Returns `501 Not Implemented` with Phase 2 messaging |
| Auth infrastructure (rate limiting hooks) | Gilfoyle | S4 | Per-user request tracking in Redis — Phase 2 adds AI-specific limits |

---

## 2. Epic Breakdown

### E1: Project Scaffolding

**Owner:** Gilfoyle (backend) + Dinesh (frontend)
**Effort:** 4 days
**Sprint:** S1
**Dependencies:** None (starting point)

**Description:** Stand up the full development environment. Aspire orchestrates .NET API, PostgreSQL, and Redis. React Native app bootstrapped with React Native Paper, navigation shell, and theme system.

**Deliverables:**

- [ ] .NET 10 Minimal API project with Aspire `AppHost` and `ServiceDefaults`
- [ ] PostgreSQL container configured in Aspire with connection strings
- [ ] Redis container configured in Aspire for caching
- [ ] React Native app initialized with Expo or bare workflow
- [ ] React Native Paper installed with Material Design 3 theme (dark + light)
- [ ] Bottom tab navigation shell (Home, Seasons, Drivers, Constructors, Settings)
- [ ] Health check endpoint (`GET /health`) returning service status
- [ ] `README.md` with local dev setup instructions (`dotnet run` → everything starts)
- [ ] Git repository structure: `/src/api`, `/src/app`, `/docs`, `/scripts`

**Acceptance Criteria:**

- `dotnet run --project src/api/WeRace.AppHost` starts API + PostgreSQL + Redis
- React Native app launches on iOS simulator and Android emulator
- App displays placeholder screens with correct navigation
- Health check returns 200 with PostgreSQL and Redis connectivity confirmed

---

### E2: Data Pipeline

**Owner:** Gilfoyle
**Effort:** 5 days
**Sprint:** S1–S2
**Dependencies:** E1 (PostgreSQL must be running)

**Description:** Import Jolpica database dumps into PostgreSQL. Create the production schema, seed script, and verify data integrity. This is the foundation everything else reads from.

**Deliverables:**

- [ ] Download and analyze Jolpica dump file format (MySQL → PostgreSQL conversion if needed)
- [ ] PostgreSQL schema DDL matching Jolpica data model (tables: seasons, races, circuits, drivers, constructors, results, qualifying, lap_times, pit_stops, driver_standings, constructor_standings, status)
- [ ] Import script (`scripts/import-data.sh` or .NET CLI tool) that loads dump into PostgreSQL
- [ ] Data validation queries confirming row counts and referential integrity
- [ ] Aspire integration: seed runs automatically on first `dotnet run` in dev
- [ ] `werace_ai_readonly` PostgreSQL role created (SELECT-only on all data tables)
- [ ] Indexes on common query patterns (race lookups by season, driver results, standings by race)

**Acceptance Criteria:**

- All Jolpica data loads without errors (seasons 1950–present)
- Row counts match source dump for all tables
- Foreign key constraints pass (no orphaned results, no missing drivers)
- Query `SELECT * FROM results WHERE race_id = X` returns in < 50ms
- `werace_ai_readonly` role can SELECT but not INSERT/UPDATE/DELETE

**Open Items (Resolve During S1):**

- Jolpica dump format: MySQL SQL, CSV, or other? Transformation needed? (Q12-F2)
- Sprint race data: Does Jolpica include sprint results? If yes, which table? (Q3)
- Qualifying data: Stored in separate `qualifying` table or in `results` with type flag? (Q4)
- Pit stop data: Included in dump? If yes, import it. (Q5)

---

### E3: API Layer

**Owner:** Gilfoyle
**Effort:** 8 days
**Sprint:** S2–S3
**Dependencies:** E2 (database must be seeded)

**Description:** RESTful API endpoints for all public F1 data. Every endpoint is read-only and publicly accessible (no auth required). Consistent response shapes, pagination, filtering, and Redis caching.

**Deliverables:**

**Seasons:**

- [ ] `GET /v1/seasons` — List all seasons (filterable: `?from=1990&to=2024`)
- [ ] `GET /v1/seasons/{year}` — Season detail with race calendar
- [ ] `GET /v1/seasons/{year}/standings/drivers` — Driver standings for season
- [ ] `GET /v1/seasons/{year}/standings/constructors` — Constructor standings for season

**Races:**

- [ ] `GET /v1/races?season={year}` — Races for a season
- [ ] `GET /v1/races/{raceId}` — Race detail (circuit, date, name)
- [ ] `GET /v1/races/{raceId}/results` — Race results (finishing order, times, status)
- [ ] `GET /v1/races/{raceId}/qualifying` — Qualifying results (Q1/Q2/Q3 times)
- [ ] `GET /v1/races/{raceId}/laps?driverId={id}` — Lap times (optional driver filter)
- [ ] `GET /v1/races/{raceId}/pitstops` — Pit stop data (if available in dump)

**Drivers:**

- [ ] `GET /v1/drivers?page={n}&pageSize={s}` — Paginated driver list
- [ ] `GET /v1/drivers/{driverId}` — Driver profile
- [ ] `GET /v1/drivers/{driverId}/results?season={year}` — Results history (filterable by season)

**Constructors:**

- [ ] `GET /v1/constructors` — All constructors
- [ ] `GET /v1/constructors/{constructorId}` — Constructor profile
- [ ] `GET /v1/constructors/{constructorId}/results?season={year}` — Results history

**Circuits:**

- [ ] `GET /v1/circuits` — All circuits
- [ ] `GET /v1/circuits/{circuitId}` — Circuit detail (location, coordinates)
- [ ] `GET /v1/circuits/{circuitId}/races` — All races at circuit

**Infrastructure:**

- [ ] Consistent JSON response envelope: `{ "data": [...], "pagination": { "page", "pageSize", "total" } }`
- [ ] Pagination: cursor-based or offset, default `pageSize=20`, max `100` (resolves Q10)
- [ ] Redis caching layer with configurable TTL (historical data: 24h, current season: 1h)
- [ ] Request/response logging middleware
- [ ] Reserve `POST /v1/ai/query` → returns `501 Not Implemented` with `{ "message": "AI features coming in Phase 2" }`

**Acceptance Criteria:**

- All endpoints return correct data validated against Jolpica source
- Pagination works correctly (page 1 of drivers, page 2, etc.)
- P95 response time < 300ms for all data endpoints (with warm cache)
- Redis cache hit ratio > 80% for repeated requests
- Response shapes match API contract document (`docs/API-CONTRACTS.md`)
- No N+1 query patterns (verified via query logging)

---

### E4: Authentication

**Owner:** Gilfoyle (backend) + Dinesh (auth UI)
**Effort:** 6 days
**Sprint:** S3–S4
**Dependencies:** E1 (API project), E3 partially (middleware patterns established)

**Description:** .NET Identity integration with email/password registration and login. Passkey support (FIDO2/WebAuthn). Anonymous access preserved for all public endpoints. Auth gate scaffolded for future AI/telemetry endpoints.

**Deliverables:**

**Backend (Gilfoyle):**

- [ ] .NET Identity configured with PostgreSQL identity store
- [ ] `POST /v1/auth/register` — Create account (name, email, password)
- [ ] `POST /v1/auth/login` — Email/password login → returns JWT
- [ ] `POST /v1/auth/refresh` — Token refresh
- [ ] `POST /v1/auth/logout` — Invalidate session
- [ ] Passkey registration and authentication endpoints (WebAuthn challenge/response)
- [ ] JWT middleware: all `/v1/ai/*` and `/v1/telemetry/*` routes require Bearer token
- [ ] Public routes (`/v1/seasons`, `/v1/races`, etc.) remain anonymous-accessible
- [ ] Per-user request counter in Redis (key: `user:{id}:requests:{date}`) — Phase 2 uses for rate limiting

**Frontend (Dinesh):**

- [ ] Login screen (email/password form)
- [ ] Registration screen (name, email, password, confirm password)
- [ ] Passkey enrollment UI (biometric prompt)
- [ ] Auth state management (secure token storage, auto-refresh)
- [ ] Anonymous → authenticated transition: when user taps a gated feature, show login modal
- [ ] Settings screen: logged-in status, logout button, account info
- [ ] Auth error handling (401 → redirect to login, expired token → refresh)

**Acceptance Criteria:**

- New user can register with email/password and immediately access authenticated endpoints
- Login persists across app restarts (secure token storage)
- Anonymous user can browse all public data without login prompt
- Tapping a gated feature (placeholder AI button) triggers login flow
- Passkey registration and login work on iOS (Passkeys API) and Android (Credential Manager)
- Invalid credentials show clear error messages
- JWT expiry and refresh work transparently

**Open Item:**

- Passkey feasibility in React Native (Q25-F4): If FIDO2 proves complex, ship email/password in S4 and add passkeys in S5 as stretch goal

---

### E5: Mobile UI

**Owner:** Dinesh
**Effort:** 10 days
**Sprint:** S2–S5 (progressive delivery)
**Dependencies:** E1 (navigation shell), E3 (API endpoints to consume)

**Description:** All user-facing screens for F1 data browsing. Built with React Native Paper components, connected to the .NET API. Responsive, performant, accessible.

**Deliverables:**

**Sprint 2 — Core Screens:**

- [ ] Home screen: current/latest season summary, quick links to seasons and drivers
- [ ] Season list screen: browse all seasons, decade filter chips
- [ ] Season detail screen: race calendar for selected season

**Sprint 3 — Race & Results:**

- [ ] Race detail screen: circuit info, date, tabs for Results / Qualifying / Laps
- [ ] Race results tab: finishing order table (position, driver, constructor, time/gap, status)
- [ ] Qualifying results tab: Q1/Q2/Q3 times grid
- [ ] Standings screen: driver and constructor standings tables with points

**Sprint 4 — Profiles & Search:**

- [ ] Driver profile screen: stats cards (wins, poles, championships), results history
- [ ] Constructor profile screen: stats, seasons active, results history
- [ ] Circuit detail screen: location, coordinates, list of races held
- [ ] Search screen: search drivers, constructors, circuits by name; filter by season
- [ ] Client-side search with backend fallback (resolves Q2 for Phase 1 — evaluate backend search in Phase 2)

**Sprint 5 — Polish:**

- [ ] Empty states for all screens: "No results available", "Season not yet started" (resolves Q7)
- [ ] Error states: network error, server error, timeout — with retry button (resolves Q29)
- [ ] Loading states: skeleton screens for all list and detail views
- [ ] Pull-to-refresh on list screens
- [ ] Off-season home screen variant: "Season starts [date]", show previous season highlights (resolves Q9)
- [ ] Accessibility pass: screen reader labels, touch target sizes (44×44pt minimum), dynamic type support

**Acceptance Criteria:**

- All screens render correctly on iPhone 14 (375pt width) and Pixel 7 (412dp width)
- Dark mode and light mode both fully themed, no unstyled components
- Navigation between any two screens takes < 300ms transition time
- Lists scroll at 60fps with no jank (tested with 50+ items)
- All list views paginate correctly (load more on scroll)
- Search returns results within 200ms for client-side, 500ms for API-backed
- VoiceOver (iOS) and TalkBack (Android) can navigate all screens

---

### E6: AI Foundations & Phase 1 Hardening

**Owner:** Gilfoyle (DB views), Richard (schema docs), Jared (test coverage)
**Effort:** 4 days
**Sprint:** S3–S5 (parallel to other work)
**Dependencies:** E2 (schema finalized), E3 (API stable)

**Description:** Prepare database and documentation artifacts that Phase 2 AI development consumes on day one. No user-facing AI features. Also includes test hardening across the full stack.

**Deliverables:**

**Database Views (Gilfoyle, S3):**

- [ ] `v_driver_career_stats` — Wins, poles, podiums, championships, career points per driver
- [ ] `v_constructor_season_stats` — Points, wins, podiums per constructor per season
- [ ] `v_race_summary` — Race name, date, winner, pole sitter, fastest lap holder
- [ ] `v_head_to_head` — Win/qualifying comparison between any two drivers who raced together
- [ ] `v_circuit_records` — Fastest lap, most wins at each circuit

**Schema Documentation (Richard, S3):**

- [ ] `docs/SCHEMA.md` — Every table, column, type, constraint, relationship documented
- [ ] Column descriptions for LLM context (what `status_id` values mean, what `position = NULL` means)
- [ ] Query cookbook: 10 example queries the AI agent will need (career wins, season comparisons, race lookups)

**Test Coverage (Jared, S3–S5):**

- [ ] Unit tests for all API endpoints (response shapes, pagination, filters)
- [ ] Integration tests for data pipeline (import → query → verify)
- [ ] E2E test skeleton: React Native + API integration (happy path for 3 core flows)
- [ ] Auth flow tests: register, login, token refresh, anonymous access, gated endpoint rejection
- [ ] Performance baseline: automated P95 latency check for top 10 endpoints

**Acceptance Criteria:**

- All database views return correct data (validated against raw queries)
- Schema documentation covers 100% of tables and columns
- Unit test coverage > 80% for API layer
- All integration tests pass in CI
- E2E tests pass for: browse season → race → results, search driver, register + login

---

## 3. Sprint Structure

**Sprint cadence:** 1-week sprints (Monday–Friday)
**Ceremonies:** Planning (Monday AM), Daily standup (15 min), Demo (Friday PM)
**Total duration:** 5 sprints = 5 weeks

### Sprint 1 — Foundation

**Theme:** Get everything running

| Task | Owner | Epic | Days |
|------|-------|------|------|
| .NET API + Aspire setup | Gilfoyle | E1 | 2 |
| PostgreSQL + Redis in Aspire | Gilfoyle | E1 | 1 |
| React Native app + Paper + nav shell | Dinesh | E1 | 2 |
| Theme system (dark/light) | Dinesh | E1 | 1 |
| Jolpica dump analysis + schema DDL | Gilfoyle | E2 | 2 |
| Import script (dump → PostgreSQL) | Gilfoyle | E2 | 2 |
| Test infrastructure setup (xUnit, Jest) | Jared | E6 | 2 |
| API contract draft (`docs/API-CONTRACTS.md`) | Richard | E3 | 2 |
| Data model decisions (Q3, Q4, Q5) | Monica + Richard | — | 1 |

**Exit Criteria:** `dotnet run` starts full stack; database seeded; app launches with nav shell.

---

### Sprint 2 — Data & First Screens

**Theme:** API endpoints + first real UI

| Task | Owner | Epic | Days |
|------|-------|------|------|
| Seasons + Races API endpoints | Gilfoyle | E3 | 3 |
| Drivers + Constructors API endpoints | Gilfoyle | E3 | 2 |
| Pagination + caching middleware | Gilfoyle | E3 | 1 |
| Home screen (connected to API) | Dinesh | E5 | 2 |
| Season list + season detail screens | Dinesh | E5 | 3 |
| API endpoint unit tests (seasons, races) | Jared | E6 | 2 |
| Data integrity validation tests | Jared | E6 | 1 |
| Create `werace_ai_readonly` DB role | Gilfoyle | E6 | 0.5 |
| Reserve `/ai` namespace (501 response) | Gilfoyle | E6 | 0.5 |
| Resolve Q10 (pagination strategy) | Monica | — | — |

**Exit Criteria:** API serves season and race data; home screen and season browser functional on device.

---

### Sprint 3 — Results & Profiles

**Theme:** Core data browsing complete

| Task | Owner | Epic | Days |
|------|-------|------|------|
| Circuits + Standings API endpoints | Gilfoyle | E3 | 2 |
| Qualifying + Laps + Pitstops endpoints | Gilfoyle | E3 | 2 |
| Database views for AI foundations | Gilfoyle | E6 | 1.5 |
| .NET Identity setup + auth endpoints | Gilfoyle | E4 | 2 |
| Race detail screen (results, qualifying, laps tabs) | Dinesh | E5 | 3 |
| Standings screen | Dinesh | E5 | 1 |
| Schema documentation (`docs/SCHEMA.md`) | Richard | E6 | 2 |
| API tests (circuits, standings, qualifying) | Jared | E6 | 2 |
| Integration tests (pipeline import) | Jared | E6 | 1 |
| Refine acceptance criteria (Q7, Q9) | Monica | — | — |

**Exit Criteria:** Full API surface live; race detail screens working; auth backend scaffolded.

---

### Sprint 4 — Auth & Search

**Theme:** Authentication + remaining screens

| Task | Owner | Epic | Days |
|------|-------|------|------|
| Auth endpoints (register, login, refresh, passkeys) | Gilfoyle | E4 | 3 |
| JWT middleware + route protection | Gilfoyle | E4 | 1 |
| Redis per-user request tracking | Gilfoyle | E4 | 1 |
| Login + registration screens | Dinesh | E4 | 2 |
| Driver profile + constructor profile screens | Dinesh | E5 | 2 |
| Search screen (client-side) | Dinesh | E5 | 2 |
| Auth flow tests (register, login, gated access) | Jared | E6 | 2 |
| E2E test skeleton (3 core flows) | Jared | E6 | 2 |
| Resolve Q2 (search architecture decision) | Monica + Richard | — | — |

**Exit Criteria:** User can register, log in, browse all data, search. Auth gates future AI/telemetry endpoints.

---

### Sprint 5 — Polish & Ship

**Theme:** Quality, edge cases, release readiness

| Task | Owner | Epic | Days |
|------|-------|------|------|
| Performance optimization (query tuning, cache hits) | Gilfoyle | E3 | 2 |
| Passkey polish / fallback if incomplete | Gilfoyle | E4 | 1 |
| Redis cache tuning + monitoring | Gilfoyle | E3 | 1 |
| Empty states, error states, loading skeletons | Dinesh | E5 | 2 |
| Off-season home screen variant | Dinesh | E5 | 1 |
| Accessibility pass (VoiceOver, TalkBack, touch targets) | Dinesh | E5 | 2 |
| Circuit detail screen | Dinesh | E5 | 1 |
| Performance baseline tests (P95 latency) | Jared | E6 | 1 |
| Full regression test pass | Jared | E6 | 2 |
| Code review + architecture sign-off | Richard | — | 2 |
| Phase 2 readiness checklist | Richard + Monica | — | 1 |

**Exit Criteria:** All acceptance criteria met. Phase 1 is shippable. Phase 2 can start immediately.

---

### Critical Path

```
E1 (Scaffolding) → E2 (Data Pipeline) → E3 (API Layer) → E5 (UI Screens)
                                            ↓
                                       E4 (Auth) → E5 (Auth UI)
                                            ↓
                                       E6 (AI Foundations)
```

**Bottleneck:** Gilfoyle is on the critical path for S1–S3 (scaffolding → data → API). Dinesh is blocked on API endpoints for connected UI. Mitigation: Dinesh builds screens with mock data in S2, connects to real API as endpoints land.

**Parallel tracks:**

- Gilfoyle: E1 → E2 → E3 → E4 → optimization (sequential, backend-heavy)
- Dinesh: E1 → E5(mock) → E5(connected) → E4(UI) → E5(polish) (can work ahead with mocks)
- Jared: E6(infra) → E6(unit) → E6(integration) → E6(e2e) → E6(perf) (continuous, never blocked)
- Richard: API contracts → schema docs → code review → architecture sign-off
- Monica: Acceptance criteria → UX decisions → Phase 2 readiness

---

## 4. Team Assignments

| Team Member | Role | Primary Responsibilities | Sprint Focus |
|-------------|------|--------------------------|--------------|
| **Gilfoyle** | Backend Engineer | API, database, Aspire, data pipeline, auth backend, AI DB prep | S1–S5: backend-heavy throughout |
| **Dinesh** | Frontend Engineer | React Native app, navigation, all screens, design system, auth UI | S1 setup, S2–S4 screens, S5 polish |
| **Jared** | Test Engineer | Test infrastructure, unit/integration/E2E tests, performance baseline | S1 setup, S2–S5 continuous testing |
| **Monica** | Product Owner | Acceptance criteria, UX decisions (Q7, Q9, Q10), Phase 2 prep | Ongoing: unblock team with decisions |
| **Richard** | Lead / Architect | API contracts, schema docs, code review, architecture oversight | S1 contracts, S3 docs, S4–S5 review |

### Ownership Matrix

| Epic | Primary | Secondary | Reviewer |
|------|---------|-----------|----------|
| E1: Scaffolding | Gilfoyle (API), Dinesh (app) | — | Richard |
| E2: Data Pipeline | Gilfoyle | — | Richard |
| E3: API Layer | Gilfoyle | — | Richard |
| E4: Authentication | Gilfoyle (backend), Dinesh (UI) | — | Richard |
| E5: Mobile UI | Dinesh | — | Monica (UX), Richard (arch) |
| E6: AI Foundations | Gilfoyle (views), Richard (docs), Jared (tests) | — | Richard |

---

## 5. Open Items to Resolve During Phase 1

These questions were raised in the PRD review and must be answered during Phase 1 sprints. Each has an owner and a deadline sprint.

| ID | Question | Owner | Decide By | Impact |
|----|----------|-------|-----------|--------|
| Q3 | Sprint races (2021+): import sprint results? What table? | Monica → Gilfoyle | S1 | Schema design |
| Q4 | Qualifying model: separate `qualifying` table or flag in `results`? | Richard → Gilfoyle | S1 | Schema design |
| Q5 | Pit stop data: in scope? Which table? | Monica → Gilfoyle | S1 | Schema design, API endpoint |
| Q2 | Search: client-side filter or backend endpoint? | Richard + Monica | S4 | API contract, frontend architecture |
| Q7 | Empty/error state designs for all screens | Monica → Dinesh | S3 | UI implementation in S5 |
| Q9 | Off-season home screen: what content? | Monica | S3 | UI implementation in S5 |
| Q10 | Pagination strategy: offset vs. cursor, page size | Richard | S2 | API contract, every list endpoint |
| Q12-F2 | Jolpica dump format: MySQL SQL or CSV? Transformation script needed? | Gilfoyle | S1 | Data pipeline |
| Q12-F5 | Sync strategy: one-time import + API delta, or periodic re-import? | Richard + Gilfoyle | S2 | Pipeline architecture |
| Q25-F4 | Passkey feasibility in React Native (FIDO2/WebAuthn) | Dinesh | S3 | Auth UI scope for S4 |

**Resolution protocol:** Owner investigates, proposes answer in daily standup, Richard approves architectural decisions, Monica approves product decisions. Decisions logged in `.squad/decisions.md`.

---

## 6. Definition of Done — Phase 1

Phase 1 is shippable when ALL of the following are true:

### Functional

- [ ] User can browse all F1 seasons (1950–present) and view race calendars
- [ ] User can view race results, qualifying results, and standings for any race
- [ ] User can view driver profiles with career statistics
- [ ] User can view constructor profiles with historical results
- [ ] User can view circuit details and race history at each circuit
- [ ] User can search for drivers, constructors, and circuits by name
- [ ] User can register with email/password and log in
- [ ] User can log in with passkeys (or: passkey support documented as S6 stretch if blocked)
- [ ] Anonymous users can browse all public data without login prompts
- [ ] Authenticated endpoints (`/ai/*`, `/telemetry/*`) return 401 for anonymous users
- [ ] Dark mode and light mode work correctly throughout the app

### Non-Functional

- [ ] API P95 response time < 300ms for all data endpoints
- [ ] App launches in < 2 seconds on mid-range device
- [ ] List views scroll at 60fps with 50+ items
- [ ] All screens have empty, loading, and error states
- [ ] VoiceOver (iOS) and TalkBack (Android) can navigate all screens
- [ ] Touch targets meet 44×44pt minimum

### Quality

- [ ] Unit test coverage > 80% for API endpoints
- [ ] All integration tests pass (data pipeline, auth flows)
- [ ] E2E tests pass for 3 core flows (browse season→race→results, search driver, register+login)
- [ ] Zero critical or high-severity bugs open
- [ ] Code review completed by Richard on all merged PRs

### AI Readiness (Phase 2 Foundation)

- [ ] `werace_ai_readonly` PostgreSQL role exists and is SELECT-only
- [ ] 5 database views created and documented
- [ ] `docs/SCHEMA.md` covers 100% of tables with column descriptions
- [ ] `/ai` namespace reserved and returns 501
- [ ] Per-user request tracking functional in Redis

---

## 7. Risks and Mitigations

### Risk 1: Jolpica Dump Format Incompatibility

**Likelihood:** Medium
**Impact:** High — blocks E2 (data pipeline), cascades to E3 and E5
**Description:** Jolpica dumps may be MySQL-format SQL that requires non-trivial transformation for PostgreSQL. Schema differences, data types, or encoding issues could add 2–3 days.

**Mitigations:**

1. Gilfoyle analyzes dump format on Day 1 of S1 (before writing any import code)
2. If MySQL format: use `pgloader` for automated MySQL-to-PostgreSQL conversion
3. If blocked > 2 days: fallback to Jolpica REST API for initial seeding (slower but unblocked)
4. Richard provides schema DDL independently so API development can proceed with synthetic test data

### Risk 2: Passkey Implementation Complexity in React Native

**Likelihood:** Medium
**Impact:** Medium — could delay auth epic by 2–3 days
**Description:** FIDO2/WebAuthn in React Native requires native module bridges to iOS Passkeys API and Android Credential Manager. Library maturity varies. May hit platform-specific bugs.

**Mitigations:**

1. Dinesh spikes passkey feasibility in S3 (before S4 auth sprint)
2. If libraries are immature: ship email/password auth in Phase 1, add passkeys as first task of Phase 2
3. Email/password alone still satisfies all Phase 1 acceptance criteria
4. Auth backend supports passkeys regardless — only frontend is at risk

### Risk 3: Gilfoyle Bottleneck on Critical Path

**Likelihood:** High
**Impact:** Medium — Dinesh blocked on real API data; schedule slides if Gilfoyle falls behind
**Description:** Gilfoyle owns E1 (partially), E2, E3, and E4 backend. This is ~21 days of work across 25 sprint days. Any delay cascades to frontend.

**Mitigations:**

1. Dinesh works with mock data and JSON fixtures for S2 screens (decoupled from API)
2. Richard publishes API contracts in S1 so Dinesh can build screens against contract, not implementation
3. Prioritize API endpoints by UI dependency: seasons/races (S2) before drivers/circuits (S3)
4. If Gilfoyle falls behind on E3: Richard picks up 1–2 simpler endpoints (circuits, status)
5. Jared writes API tests early — forces endpoint contracts to be stable

---

## Appendices

### A. API Contract Reference

Full API contracts will be documented in `docs/API-CONTRACTS.md` during S1 by Richard. Preview of response shapes:

**Paginated List Response:**

```json
{
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 857,
    "totalPages": 43
  }
}
```

**Single Resource Response:**

```json
{
  "data": {
    "id": 1,
    "year": 2024,
    "races": [ ... ]
  }
}
```

**Error Response:**

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Season 2030 not found"
  }
}
```

### B. Phase 2 Handoff Checklist

At end of Phase 1, Richard verifies these are ready for Phase 2 kickoff:

- [ ] `werace_ai_readonly` role tested with sample queries
- [ ] Database views return correct aggregations
- [ ] `docs/SCHEMA.md` reviewed by Gilfoyle for accuracy
- [ ] Query cookbook validated (all 10 example queries run correctly)
- [ ] Auth infrastructure supports per-user rate limiting
- [ ] `/ai` namespace routing ready to swap 501 for real handler
- [ ] Phase 2 plan drafted with AI epic breakdown

---

*Richard — Lead / Architect*
*"Ship the boring stuff first. Make it solid. Then add the magic."*
