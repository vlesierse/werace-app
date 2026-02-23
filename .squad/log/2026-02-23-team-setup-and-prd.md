# Team Setup & PRD Drafting — 2026-02-23

**Session ID:** team-setup-and-prd  
**Date:** 2026-02-23  
**Participants:** Squad (Coordinator), Richard (Lead), Vincent Lesierse (Owner)

## Overview

Team formation with full Silicon Valley casting for WeRace (Formula 1 mobile companion app). Richard drafted comprehensive PRD covering product vision, technical architecture, and implementation roadmap.

## Milestones

### 1. Team Formation (2026-02-23 10:00)

**Agents Spawned:**
- **Richard** (Lead) — Product vision, PRD, architecture
- **Dinesh** (Frontend) — React Native UI, design system
- **Gilfoyle** (Backend) — .NET API, database, data pipeline
- **Jared** (Tester) — QA strategy, test automation
- **Scribe** (Memory) — Logging, decision merging, cross-agent updates

**Tech Stack Finalized:**
- **Frontend:** React Native + React Native Paper (Material Design 3)
- **Backend:** .NET 10 Minimal API
- **Dev Orchestration:** Aspire
- **Database:** PostgreSQL (architectural decision)
- **AI:** RAG-based agent with LLM + SQL query generation
- **Cache:** Redis

### 2. PRD Drafting (2026-02-23 10:15 → completion)

**Owner:** Richard (Lead)  
**Output:** docs/PRD.md (21.2 KB) — comprehensive product requirements

**Content:**
- **Product Vision:** Formula 1 mobile app with historical race data, live info, AI-powered Q&A
- **User Personas:** 
  - Historian (casual fan, trivia browsing)
  - Analyst (data enthusiast, telemetry exploration)
  - Weekend Warrior (race weekend companion, live updates)
- **Feature Tiers:**
  - P0 (MVP): Historical browsing, AI Q&A, core navigation, profiles
  - P1 (Post-MVP): Race weekend companion, telemetry exploration
  - P2 (Future): Personalization, offline mode, circuit explorer
- **Technical Decisions:**
  1. **Database:** PostgreSQL for relational F1 data structure
  2. **UI Framework:** React Native Paper (Material Design 3 compliance, theming, active maintenance)
  3. **API Architecture:** RESTful with resource-based endpoints (/seasons, /races, /drivers, /constructors, /circuits, /telemetry, /ai)
  4. **AI Approach:** RAG with SQL query generation (structured data → SQL → answer formatting)
- **Data Model:** Season, Race, Driver, Constructor, Result, LapTime, TelemetryData, Standings
- **Open Questions Identified:**
  1. AI Backend: Azure OpenAI vs. self-hosted (Llama 3)
  2. Data Licensing: Ergast/OpenF1 rights verification
  3. Telemetry Source: OpenF1 vs. FastF1 vs. official FOM
  4. Monetization: Free + ads, freemium, or paid upfront

## Decisions Made

| Decision | Owner | Detail |
|----------|-------|--------|
| Database: PostgreSQL | Richard | Relational model for F1 data, SQL queryability for AI agent |
| UI: React Native Paper | Richard | Material Design 3, theming support, active maintenance, React Native community standard |
| API: RESTful | Richard | Mobile-friendly, predictable, cacheable, aligns with React Native patterns |
| AI: RAG + SQL | Richard | Accuracy via structured retrieval, verifiable results, avoids LLM hallucination |

## Dependencies & Cross-Agent Impact

- **Dinesh (Frontend)** → Must implement React Native + React Native Paper per PRD specs
- **Gilfoyle (Backend)** → Must design PostgreSQL schema per data model, implement RESTful endpoints
- **Jared (Tester)** → Must develop QA strategy and test automation for P0 features
- **All Agents** → Open questions (AI backend, data licensing) require team input before full backend implementation

## Files Created/Modified

| File | Status | Notes |
|------|--------|-------|
| docs/PRD.md | Created | 21.2 KB, comprehensive spec |
| .squad/agents/richard/history.md | Updated | Linked PRD, documented learnings |
| .squad/decisions/inbox/richard-prd-draft.md | Created | 4 architectural decisions, 3 feature tiers, 4 open questions |

## Next Session

1. **Dinesh:** React Native project setup, React Native Paper integration, core navigation structure
2. **Gilfoyle:** PostgreSQL schema design, Aspire container setup, baseline API endpoints
3. **Jared:** QA strategy definition, test framework selection, P0 test case mapping
4. **Richard:** Open question research (data licensing, AI backend evaluation)

## Session Notes

- Team is fully staffed and aligned on vision
- PRD provides complete specification for parallel implementation
- No blockers identified at team level
- Open questions delegated for research post-PRD
