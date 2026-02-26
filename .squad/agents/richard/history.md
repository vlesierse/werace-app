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
