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

**You Can Start:**
- Setting up PostgreSQL schema with Season, Race, Circuit, Driver, Constructor, Result, LapTime, DriverStanding, ConstructorStanding entities
- Designing Minimal API endpoint structure and request/response contracts
- Planning ETL pipeline (awaiting data source confirmation)

**Full Details:** See `docs/PRD-REVIEW.md` (36 questions with full context) and `.squad/decisions.md` (merged PRD review findings)
