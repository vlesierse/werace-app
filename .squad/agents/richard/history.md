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
