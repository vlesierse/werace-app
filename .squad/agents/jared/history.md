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
