# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD drafted
- **Document:** `docs/PRD.md` — comprehensive product requirements for WeRace app
- **Scope Definition:** P0 (MVP) = historical data + AI Q&A + core mobile nav | P1 = race weekend companion + telemetry | P2 = personalization + offline
- **Architecture Decision:** PostgreSQL for relational F1 data (seasons → races → results), Redis for caching, Azure OpenAI (or similar) for AI agent
- **UI Framework:** React Native Paper (Material Design 3) — most popular, well-maintained, excellent theming
- **Data Model:** Core entities defined (Season, Race, Driver, Constructor, Result, LapTime, TelemetryData, Standings)
- **API Design:** RESTful endpoints grouped by resource (/seasons, /races, /drivers, /constructors, /circuits, /telemetry, /ai)
- **AI Agent:** RAG approach with SQL query generation for structured F1 data, vector search for future enhancement
- **Open Questions:** Data licensing (Ergast/OpenF1 rights), AI backend choice (Azure vs self-hosted), telemetry data sources, monetization model
- **User Personas:** The Historian (casual fan, trivia), The Analyst (data enthusiast, telemetry), The Weekend Warrior (race weekend companion)
- **Key File Paths:** `/docs/PRD.md`

### 2026-02-23: PRD Review Complete — Development Blocked on Critical Decisions

**Status:** 🟡 Monica (Product Owner) completed PRD review; 36 questions raised; 4 critical blockers identified

**Your Role:** As Lead, you need to help Vincent prioritize answers to critical questions (Q1, Q12, Q18, Q25) before development can start.

**Critical Blockers (Must Resolve Before Dev):**
1. **Q25 — Authentication:** No auth model specified; blocks rate limiting, conversation persistence, monetization architecture
2. **Q12 — Data Source:** Ergast API is deprecated (shut down 2024); need confirmed replacement (static dump? Alternative API? FOM partnership?)
3. **Q18 — AI Security:** LLM-generated SQL without guardrails = SQL injection risk; need query validation, allowlist, read-only enforcement
4. **Q1 — MVP Scope:** Three major features (data browsing + AI agent + core nav) may be too ambitious; recommend phasing to reduce risk

**Data Model Gaps (Need Decision):**
- Qualifying results: stored in Result with flag, or separate entity?
- Sprint races/Sprint Shootouts (2021+): in scope?
- Pit stop data: in scope for MVP?
- Search: feature exists but no API endpoint defined

**Architecture Impact:**
- Cannot finalize database schema until Q3, Q4, Q5 (qualifying, sprint, pit stops) are answered
- Cannot design API contracts without clarity on Q2 (search architecture)
- AI agent cannot move forward without Q18 (SQL injection guardrails), Q20 (rate limiting), Q22 (content boundaries)

**Full Details:** See `docs/PRD-REVIEW.md` (36 questions, 8 vague requirements, 10 recommendations) and `.squad/decisions.md` (merged findings)

**Session Log:** `.squad/log/2026-02-23-prd-review.md` — full context of PRD review outcome

### 2026-02-26: Q12 Data Source Resolved — Jolpica Replaces Ergast (cross-agent)

**Status update:** 2 of 4 critical blockers now resolved (Q25 Authentication, Q12 Data Source). Remaining: Q18 (AI safety rails), Q1 (MVP scope).

**Q12 Resolution:** Jolpica API confirmed as drop-in Ergast replacement. Provides Ergast-compatible API + database dump files for direct PostgreSQL import. Pipeline approach is bulk dump import rather than incremental scraping.

**6 follow-up questions filed** (F1–F6) covering dump format, sync strategy, licensing, data freshness, completeness, and 2025+ coverage. See `.squad/decisions.md` for details.

**Next priority blockers:** Q18 (AI safety rails) and Q1 (MVP scope assessment).
### 2026-02-26: Blocker Brainstorm — Q18 (AI Safety) and Q1 (MVP Scope)

**What:** Wrote architectural analysis for the two remaining critical blockers. Filed to `.squad/decisions/inbox/richard-blocker-brainstorm.md` for Monica to synthesize.

**Q18 — AI Safety Rails positions taken:**
- **Defense in depth:** Read-only PostgreSQL user (non-negotiable) + schema-aware system prompts + SQL validation middleware in .NET + execution time/row limits. Four layers stacked.
- **Against strict query allowlist for MVP:** Too restrictive, kills the natural language value proposition. Can tighten post-launch if abuse patterns emerge.
- **Rate limiting:** API middleware with Redis-backed per-user counters (not DB-level, not gateway-level). 50/day, 10/min burst. Azure OpenAI budget caps as cost ceiling.
- **Content boundaries:** F1-only, historical-only, no speculation, no personal data. Enforced via Azure content filters + system prompt + .NET response validation.

**Q1 — MVP Scope positions taken:**
- **Defer AI agent from MVP.** Ship data browsing + navigation + auth as MVP-lite. AI agent as 2-3 week fast-follow.
- **Clean separation confirmed:** AI agent is additive (sits on top of data layer), not entangled with browsing. No schema or API contract changes needed to defer.
- **Phase 1 foundations:** Build DB views for common queries, auth infrastructure, Redis, schema documentation — all serve Phase 2 AI with zero waste.
- **Key argument:** Validate "do F1 fans want a mobile data app?" before investing in AI. The data browsing app is a real product on its own.

### 2026-02-26: Phase 1 Plan Created

**What:** Authored `docs/PHASE1-PLAN.md` — comprehensive sprint plan for Phase 1 execution. Filed architectural decisions to `.squad/decisions/inbox/richard-phase1-plan.md`.

**Plan Structure:**
- **Timeline:** 5 sprints × 1 week = 5 weeks
- **6 Epics:** E1 Scaffolding → E2 Data Pipeline → E3 API Layer → E4 Authentication → E5 Mobile UI → E6 AI Foundations
- **Critical path:** Gilfoyle (E1→E2→E3→E4), Dinesh decoupled via mock data and API contracts

**Key Architectural Decisions:**
- Offset-based pagination (pageSize=20, max 100) — simpler than cursor for static historical data
- Consistent JSON response envelope (`{ "data", "pagination", "error" }`) across all endpoints
- Client-side search for Phase 1 (datasets small enough); backend search evaluated in Phase 2
- 5 database views as zero-cost AI foundations (`v_driver_career_stats`, `v_constructor_season_stats`, `v_race_summary`, `v_head_to_head`, `v_circuit_records`)
- Redis dual-purpose: response caching (24h historical, 1h current) + per-user request counters
- Mock data strategy to decouple frontend from backend timeline
- Passkeys best-effort; email/password is the Phase 1 baseline

**Risks Identified:**
1. Jolpica dump format unknown — mitigated by Day 1 analysis + pgloader fallback
2. Passkey React Native maturity — mitigated by email/password baseline
3. Gilfoyle single-threaded on critical path — mitigated by mock decoupling + Richard as backup on simpler endpoints

### 2026-02-26: E1 Project Scaffolding — Complete

**Status:** ✅ All 3 agents delivered successfully. PR #8 opened (closes #1).

**Results:**
- Gilfoyle: .NET 10 backend (AppHost + ServiceDefaults + Api) with Aspire, PostgreSQL, Redis. 0 warnings, 0 errors.
- Dinesh: Expo SDK 55 app with Paper MD3, 5-tab navigation, dark/light theme with AsyncStorage persistence.
- Jared: xUnit test project + Jest scaffold + `docs/TESTING.md`. 1/1 tests pass.
- Full solution builds clean. Test project wired in. Committed to `squad/1-project-scaffolding`.

**10 open items tracked** with owners and sprint deadlines (Q3, Q4, Q5, Q2, Q7, Q9, Q10, Q12-F2, Q12-F5, Q25-F4).