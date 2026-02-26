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
