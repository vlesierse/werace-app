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

### 2026-02-23: PRD Review Complete — You Can Unblock on UX Decisions

**Status:** 🟡 Development blocked on 4 critical questions (Q1, Q12, Q18, Q25), but you can proceed on UX work

**You Can Start Now:**
1. **Empty/Error States (Q7, Q9, Q29):** Design screens for no internet, no data, race not happened, off-season Home
2. **Pagination Strategy (Q10):** Define infinite scroll vs load-more vs paginated pages for 800+ driver lists
3. **UI Consistency:** Begin component library using React Native Paper, establish design system

**You Need Answers For:**
- **Q11:** Circuit images sourcing (external, custom, map pin?)
- **Q17:** Driver photos and team logos sourcing (licensed assets?)
- **Q8:** Deep linking support? (share specific race results or driver profiles)
- **Q6:** AI conversation persistence? (can users revisit past conversations?)

**Data Model Additions (Will Affect Screens):**
- Qualifying results screens (separate entity or Result table flag?)
- Sprint race results (2021+ weekend format)
- Pit stop data display (if in scope)

**Accessibility Target:** WCAG 2.1 AA (standard for mobile) — see Q27 in `docs/PRD-REVIEW.md`

**Full PRD Review:** See `docs/PRD-REVIEW.md` (36 questions, vague requirements, recommendations) and `.squad/decisions.md`
