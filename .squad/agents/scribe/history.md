# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23: PRD Review Complete — 36 Questions Raised

**Status:** 🟡 Development blocked on critical blockers until resolved

**PRD Review Output:** `docs/PRD-REVIEW.md` — Monica (Product Owner) completed comprehensive review

**36 Stakeholder Questions** organized by topic:
- Scope & Priority (Q1–Q5): MVP scope, search API, sprint races, qualifying data model, pit stops
- User Experience (Q6–Q11): AI conversation persistence, empty states, deep linking, off-season Home, pagination, content sourcing
- Data & Content (Q12–Q17): **Data source (Ergast deprecated)**, seeding, result latency, incomplete data, storage format, media assets
- AI Agent Behavior (Q18–Q24): **SQL injection guards**, backend downtime fallback, rate limiting, confidence scoring, content boundaries, follow-ups, context window
- Non-Functional Requirements (Q25–Q31): **No authentication model**, iOS/Android versions, WCAG level, app size, offline behavior, concurrent users, latency targets
- Business & Legal (Q32–Q36): Monetization timing, **trademark clearance**, analytics SDK, privacy policy, app store ratings

**Critical Blockers (Must Resolve Before Development):**
1. **Q25:** No authentication model → blocks rate limiting, conversation persistence, monetization
2. **Q12:** Ergast API deprecated → data source undefined
3. **Q18:** AI lacks SQL injection guardrails → security risk
4. **Q1:** MVP may be overscoped → recommend data browsing first, AI agent fast-follow

**8 Vague Requirements Flagged:**
- "Smooth scrolling" → needs frame rate target
- "Instant feedback" → needs millisecond threshold
- "High contrast ratios" → needs WCAG level spec
- "Scalable fonts" → needs dynamic type definition
- "Speed" (design principle) → needs FCP/TTI/navigation targets
- "Comprehensive database" → needs season coverage spec
- "Understandable telemetry" → needs user knowledge level
- "Concise answers" → needs word count/format spec

**10 Recommendations (Prioritized):**
- Before dev: resolve auth (Q25), confirm data source (Q12), define AI safety (Q18), scope MVP (Q1)
- Before design: define empty states (Q7–Q9), resolve content (Q11, Q17), clarify pagination (Q10)
- Before beta: legal review (Q33–Q36), analytics SDK (Q34), accessibility (Q27)

**Data Model Gaps Identified:**
- Qualifying results not modeled (Result table flag or separate entity?)
- Sprint races/Sprint Shootouts not in schema (introduced 2021+)
- Pit stop data not modeled
- Need to add Search API endpoint definition

**Next Action:** Vincent must respond to all 36 questions in `docs/PRD-REVIEW.md`. Critical path: Q1, Q12, Q18, Q25.

**Documentation Files:** See `.squad/decisions.md` for merged PRD review findings, `.squad/log/2026-02-23-prd-review.md` for session log
