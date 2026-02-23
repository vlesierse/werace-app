# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD finalized (cross-agent)
- **Document:** `docs/PRD.md` — comprehensive product requirements for WeRace app
- **UI Framework Decision:** React Native Paper (Material Design 3) — use for all components
- **P0 Features:** Historical data browsing, AI Q&A, core navigation with dark/light mode, driver/team profiles
- **Data Model:** Seasons, Races, Drivers, Constructors, Results, LapTimes, Standings (see PRD for schema details)
- **User Personas:** Historian (casual fan), Analyst (data enthusiast), Weekend Warrior (race companion)
- **Open Questions:** AI backend provider, data licensing, telemetry sources, monetization model (see .squad/decisions.md)
