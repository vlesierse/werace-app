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
