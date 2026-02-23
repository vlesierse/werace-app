# Session Log: PRD Review

**Date:** 2026-02-23  
**Session Topic:** Product Requirements Document review and stakeholder question synthesis  
**Participants:** Monica (Product Owner), team context from Richard (Lead)  
**Duration:** 1 hour (orchestrated as background task)  
**Outcome:** 🟡 PRD requires stakeholder clarification before development

---

## What Happened

Monica reviewed the complete PRD (v1.0) against product completeness criteria and synthesized 36 stakeholder questions to close gaps before development.

### Review Scope

The review examined:
- **Goals & Non-Goals:** Clarity and feasibility
- **User Personas:** Grounding for features
- **Features (P0/P1/P2):** Completeness, acceptance criteria, edge cases
- **Technical Architecture:** Soundness of tech choices
- **Data Model:** Entity completeness, relationships
- **API Surface:** Endpoint coverage, parameter clarity
- **AI Agent:** Capabilities, safety, cost control, error handling
- **Success Metrics:** Measurability and ambition level
- **Open Questions:** Existing gaps vs. new gaps discovered

### Key Decisions & Findings

#### What's Working

1. **Architecture:** PostgreSQL for relational F1 data is sound; REST API with resource-based grouping is appropriate for mobile.
2. **Prioritization:** P0/P1/P2 breakdown provides clear scope boundaries.
3. **AI Approach:** RAG with SQL generation is better than pure LLM hallucination, but needs guardrails.
4. **Data Model:** Covers races, drivers, constructors, standings, lap times — good foundation.
5. **Design Framework:** React Native Paper with Material Design 3 is the right choice for cross-platform consistency.

#### Critical Gaps Found

**Architectural:**
- No authentication model → blocks rate limiting, conversation persistence, feature gating, monetization
- Ergast API (primary data source) is deprecated → need confirmed replacement
- AI agent lacks SQL injection guards, rate limits, content boundaries, error fallbacks

**Product:**
- MVP scope may be too large (3 major features) → recommend MVP-lite: data browsing + nav, AI agent as fast-follow
- Missing data model entities: Qualifying results, Sprint races, PitStop data
- Search is a feature but has no API endpoint definition

**UX:**
- No empty/error states specified (no internet, no data, race not yet happened, off-season Home Screen)
- Pagination undefined for 800+ driver lists
- Circuit images and driver photos sourced from where?

**Non-Functional:**
- No accessibility WCAG conformance level specified
- No app size budget, concurrent user targets, P99 latency targets
- Offline mode is P2 but no network-off behavior defined for P0

**Business & Legal:**
- Trademark clearance for "WeRace" name not confirmed
- Privacy policy missing (required by App Stores)
- Data licensing with FOM needs legal review
- Analytics SDK choice not decided

### The 36 Questions

Monica organized questions into 6 categories:

1. **Scope & Priority (5 questions):** MVP tightness, search architecture, sprint races, qualifying data model, pit stop data
2. **User Experience (6 questions):** AI conversation persistence, empty states, deep linking, off-season Home, pagination, content sourcing
3. **Data & Content (6 questions):** Data source (Ergast deprecated), seeding strategy, result latency, incomplete data handling, time storage format, media assets
4. **AI Agent Behavior (7 questions):** SQL injection guards, backend downtime fallback, rate limiting & costs, confidence scoring, content boundaries, follow-up suggestions, context window limits
5. **Non-Functional Requirements (7 questions):** Authentication, iOS/Android version targets, WCAG level, app size, offline behavior, concurrent users, latency targets
6. **Business & Legal (5 questions):** Monetization architecture timing, trademark clearance, analytics SDK, privacy policy, app store ratings

Each question includes context about why it matters and what decision is needed.

### Vague Requirements Flagged

8 statements in the PRD that lack measurable specificity:
- "Smooth scrolling" → needs frame rate target
- "Instant feedback" → needs millisecond threshold
- "High contrast ratios" → needs WCAG level
- "Scalable fonts" → needs dynamic type support definition
- "Speed" (design principle) → needs FCP, TTI, navigation time targets
- "Comprehensive database" → needs season coverage definition
- "Understandable telemetry" → needs user knowledge level specification
- "Concise answers" → needs word count or format spec

### Recommendations (10 Total)

**Before Development Starts (4 items):**
1. Resolve authentication model (Q25) — architectural foundation
2. Confirm data source (Q12) — Ergast is gone, what's next?
3. Define AI safety rails (Q18, Q20, Q22) — security + cost control
4. Scope MVP tighter (Q1) — reduce risk with data browsing first

**Before Design Starts (3 items):**
5. Define empty/error states (Q7, Q9, Q29) — every screen needs these
6. Resolve content dependencies (Q11, Q17) — circuit images, driver photos
7. Clarify pagination (Q10) — affects every list view

**Before Beta (3 items):**
8. Legal review (Q33, Q35, Q36) — trademark, privacy, licensing
9. Analytics instrumentation (Q34) — can't measure success metrics without it
10. Accessibility conformance (Q27) — WCAG AA is standard for mobile

---

## Cross-Team Impact

### All Agents Must Know

- **Development is blocked** on any P0 feature until Q1, Q12, Q18, Q25 are resolved
- **Richard (Lead)** needs to prioritize helping Vincent answer these 4 critical questions
- **Dinesh (Frontend)** can unblock on Q7, Q9, Q29 (empty/error states) and Q10 (pagination) while awaiting critical decisions
- **Gilfoyle (Backend)** cannot finalize API contracts or database schema until Q3 (Sprint races), Q4 (qualifying), Q5 (pit stops) are answered
- **Jared (Tester)** cannot write acceptance criteria until features have clear success conditions

### Next Steps

1. Vincent reviews all 36 questions in `docs/PRD-REVIEW.md`
2. Vincent provides written answers (can be inline in the same document or a response doc)
3. Monica synthesizes answers into a "PRD Clarifications" document
4. Richard unblocks team with clear architecture and feature requirements
5. Teams proceed with respective domains (design, backend, frontend, testing)

---

## Files Modified/Created

| Path | Type | Purpose |
|------|------|---------|
| `docs/PRD-REVIEW.md` | New | 36 questions, 8 vague requirements, 10 recommendations |
| `.squad/decisions/inbox/monica-prd-review.md` | New | Decision record with blockers |
| `.squad/orchestration-log/2026-02-23T10-37-monica.md` | New | This orchestration log |
| `.squad/agents/monica/history.md` | Updated | Appended learnings about PRD review |

---

## Status

✅ **PRD Review Complete** — 36 questions ready for stakeholder input  
⏸️ **Development Blocked** — waiting for clarifications on Q1, Q12, Q18, Q25  
🟡 **PRD Status:** Draft → Needs Stakeholder Dialogue

*Monica's motto: "Ambiguity is the root of all rework."*
