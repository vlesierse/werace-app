# PRD Review — WeRace

**Reviewer:** Monica (Product Owner)  
**Document Reviewed:** `docs/PRD.md` v1.0  
**Review Date:** 2026-02-23  
**Status:** 🟡 Needs Clarification Before Development

---

## Executive Summary

### What's Good

The PRD is strong for a first draft. It has clear goals and non-goals, well-defined personas that ground the feature set, a detailed data model, a thoughtful API surface, and explicit P0/P1/P2 prioritization. The open questions section shows self-awareness of unresolved decisions. The technical architecture diagram and stack choices are well-reasoned and documented.

### What Needs Work

The PRD has significant gaps in **acceptance criteria**, **edge case handling**, **non-functional requirements**, and **data lifecycle**. Several features are described at the "what" level but not the "how" level — which means developers will have to make product decisions on the fly. The AI agent section lacks safety rails, cost controls, and failure modes. There is no mention of user authentication, privacy policy, or analytics — all of which affect architecture. The MVP scope may be too large for a first release.

---

## Stakeholder Questions

### Scope & Priority

**Q1.** The MVP includes three major features: historical data browsing, AI-powered Q&A, and core mobile navigation. That's ambitious. **Could we ship an MVP with just data browsing + navigation, and add the AI agent as a fast-follow?** What's the minimum feature set that validates the product hypothesis?

**Q2.** Search is listed under P0 ("Find races, drivers, teams by name or year") but there's no search endpoint in the API surface. **Is search a simple client-side filter, or does it need a dedicated backend endpoint?** If backend, what fields are searchable? Fuzzy matching? Autocomplete?

**Q3.** Sprint races and Sprint Shootouts (introduced 2021+) are not mentioned anywhere in the data model or features. **Should the app support sprint race results and sprint weekends?** This affects the Race entity, the Results model, and the Race Weekend Companion.

**Q4.** The PRD mentions "Qualifying results" as a feature and API endpoint (`/races/{race_id}/qualifying`), but there is no Qualifying entity in the data model. **Is qualifying data stored in the Result table with a flag, or does it need its own entity?** Same question for practice sessions.

**Q5.** Pit stop data is mentioned in the Race Results feature ("pit stops") but there is no PitStop entity in the data model. **Is pit stop data in scope for MVP?** If so, we need a data model for it.

### User Experience

**Q6.** The AI chat interface is described as "Chat interface with message bubbles, quick action suggestions." **Is the AI conversation persistent across app sessions?** Can users revisit past conversations? Is there a conversation list or is it a single active thread?

**Q7.** **What should the app display before any data is loaded?** Empty states for: no internet, no data for a season, no results yet for a race that hasn't happened, no telemetry available. These need design decisions.

**Q8.** **Should the app support deep linking?** For example, sharing a link to a specific race result or driver profile. This affects URL structure and navigation architecture.

**Q9.** The Home Screen shows "upcoming race" — **what does the Home Screen look like during the off-season** (December–February) when there's no upcoming race?

**Q10.** Pagination is not specified for any list view. **What's the pagination strategy for lists with 800+ drivers or 1000+ races?** Infinite scroll? Load more button? Paginated pages? What's the page size?

**Q11.** The "Race Details" screen mentions "Hero image (circuit)" — **where do circuit images come from?** Are these sourced externally, custom-made, or just a map pin? This is a content dependency that could block the UI.

### Data & Content

**Q12.** ✅ **RESOLVED** — The Ergast API, listed as the primary data source, has been deprecated and was shut down in 2024. **What is the actual data source for historical F1 data now?** Is there a replacement API, or are we working from a static dataset (the Ergast database dump)?

> **Vincent's Answer (2026-02-26):** "Ergast is deprecated. However Jolpica is a project which builds upon Ergast and provides a compatible API. They expose the database as dump files which we would be able to import."
>
> **Resolution:** Jolpica API (Ergast-compatible) replaces Ergast as primary historical data source. Database dump files are available for direct PostgreSQL import, which simplifies the data pipeline (dump import rather than API scraping). This resolves the data source question but opens sub-questions — see `.squad/decisions/inbox/monica-data-source-followups.md`.

**Q13.** **How is the database initially seeded?** The Data Pipeline Spec is listed as TBD. We need to know: is this a one-time import, a scheduled sync, or manual data entry? This affects MVP delivery timeline.

**Q14.** **How quickly after a race ends should results appear in the app?** Real-time (minutes)? Same day? Next day? This determines whether we need a live data pipeline or batch updates.

**Q15.** Historical data completeness varies wildly. 1950s–1970s data is sparse (no lap times, no qualifying order for some races). **What's the expected behavior when data is incomplete?** Show what we have with a disclaimer? Hide incomplete fields? This affects every list and detail screen.

**Q16.** The data model stores `time` as a `string` in both Result and LapTime. **Shouldn't this be stored as milliseconds (int) for computation and display formatting done at the API/UI layer?** The LapTime entity has both — is this intentional duplication?

**Q17.** Driver photos and team logos are not mentioned in the data model or content plan. **Where do driver photos and team logos come from?** Are they in scope for MVP? These are typically licensed assets.

### AI Agent Behavior

**Q18.** The AI agent uses "LLM to generate SQL queries from natural language." **What are the security guardrails against SQL injection or destructive queries?** Is the LLM limited to read-only queries? Is there a query allowlist? Is the SQL validated before execution?

**Q19.** **What happens when the AI backend (Azure OpenAI) is unavailable?** Is there a fallback (cached responses, graceful degradation, error message)? What's the acceptable downtime?

**Q20.** AI queries have real cost (tokens). **Is there a rate limit per user? Per session?** What's the monthly AI cost budget? Without user authentication (see Q27), how do we even track per-user usage?

**Q21.** The response includes a `confidence` score. **How is confidence calculated?** What threshold determines "I'm not sure" vs. a definitive answer? Is this a hallucination guard or an LLM self-assessment?

**Q22.** **What topics are explicitly out of bounds for the AI agent?** Only F1 data, or also F1 opinions/predictions? Can it answer "Who is the GOAT?" or should it refuse subjective questions? What about questions about driver personal lives, controversies, or accidents with injuries?

**Q23.** The response format includes `follow_up_suggestions`. **Are these generated by the LLM or templated?** How many suggestions? What if the query has no natural follow-ups?

**Q24.** **What is the maximum conversation length before the context window fills up?** When the LLM context is exhausted, does the conversation restart, summarize, or trim old messages?

### Non-Functional Requirements

**Q25.** ✅ **RESOLVED** — **Is there user authentication?** The entire PRD has no mention of login, accounts, or user identity. If it's anonymous: how do we track AI usage, enforce rate limits, store conversation history, or gate premium features (monetization)? If it has auth: which provider? Email? Social login? Apple Sign-In (required by iOS)?

> **Vincent's Answer (2026-02-23):** "We use a simple authentication mechanism for users using .NET Identity with the option to use name/email + password and/or use passkeys. However logging in is optional for browsing the races, getting basic (cached) information. When the user wants telemetry or using the AI agent, logging in is required."
>
> **Resolution:** Auth provider is .NET Identity. Two login methods: email/password and passkeys. Anonymous access allowed for race browsing and cached historical data. Authenticated access required for telemetry data and AI agent. This resolves the core auth question but opens sub-questions — see `docs/PRD.md` Authentication & Authorization section and `.squad/decisions/inbox/monica-auth-followups.md`.

**Q26.** **What iOS and Android versions are supported?** React Native version determines this. Minimum iOS 15? Android API 26? This affects UI capabilities and library compatibility.

**Q27.** Accessibility says "screen reader support, scalable fonts" but **what WCAG conformance level is the target?** WCAG 2.1 AA is standard for mobile apps. Are we committing to that? Are there specific accessibility requirements for data tables (race results, standings)?

**Q28.** **What is the app size budget?** The data model suggests a large database — are we keeping all data server-side, or bundling any data with the app? React Native apps can balloon quickly.

**Q29.** **What happens when the user has no network connection (before P2 Offline Mode)?** Blank screens? Cached last-viewed data? Error message? This is a day-one UX decision.

**Q30.** **What's the target for concurrent users?** Success metrics mention DAU/MAU but not absolute numbers. 1,000 users? 100,000? 1 million? This fundamentally changes infrastructure decisions, especially for the AI agent.

**Q31.** The API response time target is P95 < 300ms for data queries. **Does this include the database query time?** What about cold starts on Azure Container Apps? Is there a P99 target?

### Business & Legal

**Q32.** Monetization is listed as an open question, but **freemium architecture affects MVP**. If we're gating features later, we need user accounts and a feature flag system from the start. **Should we build the auth + feature gating infrastructure now, even if monetization is deferred?**

**Q33.** **"WeRace" — has the name been cleared for trademark?** F1 and FIA are aggressive about IP. Does the app name, logo, or any branding reference F1 trademarks in ways that need licensing?

**Q34.** **What analytics do we need at launch?** The Success Metrics section lists DAU/MAU, session length, screens per session, return rate, feature adoption. **Which analytics SDK will we use?** (Firebase Analytics, Mixpanel, Amplitude, etc.) This needs to be instrumented from day one.

**Q35.** **Is there a privacy policy?** GDPR, CCPA, and App Store requirements demand one. If the AI agent stores conversation history, what's the data retention policy? Can users delete their data?

**Q36.** **What app store categories and ratings apply?** Are there age restrictions? Content ratings? Any content that could be flagged (accident descriptions, controversial incidents)?

---

## Vague Requirements Flagged

| # | Statement in PRD | Issue | What's Needed |
|---|---|---|---|
| V1 | "Smooth scrolling, optimized for mobile screens" | "Smooth" is not measurable | Target frame rate (60fps?), maximum list render time, jank budget |
| V2 | "Instant feedback" | "Instant" is not measurable | Define in milliseconds (< 100ms for UI feedback? < 16ms for touch response?) |
| V3 | "High contrast ratios" | Not specific | WCAG AA minimum (4.5:1 for text, 3:1 for large text)? Or AAA? |
| V4 | "Scalable fonts" | Not specific | Support dynamic type (iOS) and font scaling (Android)? Up to what scale factor? |
| V5 | "Speed" (Design Principle #3) | Entire principle is vague | Break down into: first contentful paint, time to interactive, navigation transition time |
| V6 | "Comprehensive, searchable database" | "Comprehensive" is undefined | Which seasons have full coverage? What's the minimum data per race? |
| V7 | "Make telemetry data accessible and understandable" | What does "understandable" mean? | Define the target user's F1 knowledge level. What explanations are needed? |
| V8 | "Concise text answers" | How concise? | Max word count? 1 sentence? 1 paragraph? Should it vary by query type? |

---

## Recommendations

### Before Development Starts

1. ✅ **~~Resolve authentication model (Q25)~~** — RESOLVED. .NET Identity with email/password + passkeys. Anonymous browsing allowed; auth required for telemetry and AI agent. See PRD § Authentication & Authorization.

2. ✅ **~~Confirm data source (Q12)~~** — RESOLVED. Jolpica API (Ergast-compatible) replaces Ergast as primary historical data source. Database dump files available for direct PostgreSQL import. See `.squad/decisions/inbox/monica-data-source-followups.md` for follow-up questions.

3. **Define AI safety rails (Q18, Q20, Q22)** — SQL generation from untrusted user input is a security risk. Rate limiting, query validation, and content boundaries must be specified before development.

4. **Scope the MVP tighter (Q1)** — Three major features in MVP is risky. Recommend shipping data browsing + navigation first, then AI agent in a fast-follow.

5. **Add missing data model entities (Q3, Q4, Q5)** — Qualifying, Sprint, and PitStop need data models if they're in scope.

### Before Design Starts

6. **Define empty/error states (Q7, Q9, Q29)** — Every screen needs a "no data" and "no network" design.

7. **Resolve content dependencies (Q11, Q17)** — Circuit images and driver photos are content, not code. Sourcing takes time.

8. **Clarify pagination (Q10)** — Affects every list view in the app.

### Before Beta

9. **Legal review (Q33, Q35, Q36)** — Trademark clearance, privacy policy, and data licensing are launch blockers.

10. **Analytics instrumentation (Q34)** — Can't measure success metrics without analytics SDK in place.

---

*Monica — Product Owner*  
*"Ambiguity is the root of all rework."*
