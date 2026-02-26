# Stakeholder Proposals: Critical Blockers Q18 & Q1

**Date:** 2026-02-26
**Author:** Monica (Product Owner)
**For:** Vincent Lesierse (Owner)
**Input from:** Richard (Lead/Architect), Gilfoyle (Backend Dev)

---

## Blocker 1: Q18 — AI Safety Rails

### The Problem

The WeRace AI agent generates SQL queries from natural language using an LLM. The PRD specifies no security guardrails for this. Without safety rails, the app is exposed to:

- **SQL injection** — the LLM could generate harmful queries (DELETE, DROP, data exfiltration)
- **Cost blowout** — no per-user or global limits on AI query volume or token spend
- **Off-topic abuse** — users could treat the AI as a general chatbot instead of an F1 assistant
- **Resource exhaustion** — unbounded queries could lock the database

This blocks development because the team cannot build the AI feature without knowing *how safe it needs to be*. The safety level directly affects architecture, implementation effort, and user experience.

### Where Richard and Gilfoyle Agree

The team is **strongly aligned** on the fundamental approach:

| Area | Consensus |
|------|-----------|
| Read-only DB user | Non-negotiable. A dedicated `werace_ai_readonly` PostgreSQL role with SELECT-only grants. 10 minutes to configure. |
| Schema-aware prompts | Feed the LLM the exact F1 schema (tables, columns, relationships) with strict instructions: SELECT only, F1 data only, no speculation. |
| Query validation | Parse generated SQL before execution. Reject anything that isn't a single SELECT on allowed tables. |
| Execution limits | `statement_timeout` (5 seconds), forced `LIMIT 1000` on unbounded queries, dedicated connection pool. |
| Rate limiting at API level | Per-user limits enforced before the LLM call (not at the database level — by then the expensive LLM call already happened). |
| Content boundaries | F1-only, historical-only, no personal data, no predictions. Layered enforcement: Azure content filters + system prompt + response validation. |
| Template/allowlist: not yet | Both recommend deferring a strict query template system — it eliminates the "ask anything" capability that makes the AI agent compelling. |

### Where They Differ (Details Only)

| Detail | Richard | Gilfoyle |
|--------|---------|----------|
| SQL parsing method | .NET SQL parser library (`SqlParser`) | PostgreSQL `EXPLAIN` without executing (lets the DB parse it) |
| Rate limit implementation | Redis-backed middleware, 50 queries/day | .NET built-in `System.Threading.RateLimiting`, 10 queries/hour sustained |
| Schema in system prompt | Version-controlled static file | Programmatically generated from live schema |
| Biggest concern | Safety-vs-capability balance | Query correctness (wrong answers, not injection) |

These are implementation-level differences the team will resolve during development. They don't require a stakeholder decision.

### Options

#### Option A: Defense-in-Depth (Team Recommendation)

Stack four safety layers: read-only DB user + schema-aware prompts + query validation middleware + execution limits.

| Dimension | Assessment |
|-----------|------------|
| **Security** | Strong. Four independent layers — any single failure is caught by the next. The read-only DB role is the hard floor: even a total validation bypass cannot modify data. |
| **User experience** | Full natural language flexibility. Users can ask creative, surprising F1 questions. The AI feels like a knowledgeable assistant, not a search box. |
| **Implementation effort** | ~3-4 days for the validation pipeline, on top of the base AI integration (~2-3 weeks total for complete AI feature). |
| **Cost control** | Per-user rate limits (daily cap + burst limit) + global Azure OpenAI budget ceiling. Estimated cost: ~$0.01/query. At 100 queries/user/day, ~$1/user/day worst case. |
| **Risk accepted** | The LLM may occasionally generate *incorrect* SQL that runs successfully but returns wrong data. Mitigation: test suite of known Q&A pairs, logging all generated SQL for audit. |

#### Option B: Strict Template/Allowlist

Define a fixed set of query templates (e.g., "race winner by season+round," "driver career stats"). The LLM selects a template and fills parameters — no free-form SQL generation.

| Dimension | Assessment |
|-----------|------------|
| **Security** | Maximum. Zero SQL injection surface. The LLM never generates SQL — it picks from pre-approved queries. |
| **User experience** | Significantly limited. Users can only ask questions the team has anticipated. "Which wet-weather races had the most overtakes?" would fail unless someone wrote that template. The AI becomes a structured search tool, not a conversational assistant. |
| **Implementation effort** | Similar to Option A (~3-4 days), but ongoing maintenance: every new question type requires a new template. |
| **Cost control** | Same rate limiting applies. Slightly lower token usage per query (shorter prompts). |
| **Risk accepted** | Low technical risk, but high *product* risk: users find the AI disappointing and stop using it. The "wow factor" comes from unexpected questions getting good answers. |

#### Option C: Minimal Safety (Read-Only User + Prompts Only)

Rely on the read-only database user and well-crafted system prompts. Skip the query validation middleware.

| Dimension | Assessment |
|-----------|------------|
| **Security** | Adequate floor. The DB role prevents data modification. But no validation catches bad queries before execution — malformed or expensive queries still hit the database. |
| **User experience** | Same as Option A — full flexibility. |
| **Implementation effort** | Saves ~2-3 days vs. Option A (no validation pipeline to build). |
| **Cost control** | Weaker. No query complexity checks means expensive queries can still run (though `statement_timeout` kills them after 5 seconds). Token-level cost control is the same. |
| **Risk accepted** | Higher. Relies entirely on the LLM following prompt instructions and the DB role catching everything else. No defense-in-depth. If a novel attack bypasses the prompt, the only protection is the DB role. |

### Product Perspective

The AI agent is WeRace's differentiator. Without it, the app is one of many F1 reference apps. The AI needs to feel *magical* — users should be able to ask surprising questions and get good answers.

**Option A** is the sweet spot: it protects the system without crippling the experience. Option B makes the AI feel like a dropdown menu. Option C saves a few days but leaves gaps that will cost more to fix post-launch if exploited.

The query correctness concern Gilfoyle raises is real and important — but it's a quality problem we iterate on, not a security decision Vincent needs to make.

### Team Recommendation

**Option A: Defense-in-Depth.**

Both Richard and Gilfoyle independently arrived at the same layered approach. This is the team's unanimous recommendation. It provides strong security with full product flexibility, at a manageable implementation cost.

### What Vincent Needs to Decide

| # | Decision | Options | Impact |
|---|----------|---------|--------|
| 1 | **Approve the defense-in-depth safety approach?** | Yes (Option A) / No (Option B or C) | Determines AI architecture and implementation scope |
| 2 | **AI rate limits: what daily cap per user?** | Richard suggests 50/day, Gilfoyle suggests ~100/day. Pick a starting number (can be tuned later). | Affects user experience and cost. Higher = better UX, higher cost. |
| 3 | **Global cost ceiling: what's the monthly Azure OpenAI budget?** | Gilfoyle estimates ~$0.01/query. At 1,000 users × 20 queries/day = $6,000/month. Set a ceiling. | Hard stop that prevents cost disasters. |
| 4 | **Should the AI refuse prediction questions?** | Yes (historical only) / No (allow speculation with disclaimers) | "Who will win 2026?" is the #1 question users will ask. Refusing it is safe but frustrating. |

### Follow-Up Questions

- **F1:** If we allow prediction questions (Decision #4 = No), should answers carry a visible disclaimer ("This is speculation, not based on data")?
- **F2:** Should AI-generated SQL be logged permanently for audit, or rotated after N days? (Privacy vs. debugging trade-off.)
- **F3:** When a user hits their daily AI limit, what's the UX? Hard wall with countdown, or degraded mode (e.g., suggested questions only)?
- **F4:** Should we expose a "confidence score" to users ("I'm 85% sure this answer is correct"), or just present answers without qualification?

---

## Blocker 2: Q1 — MVP Scope Assessment

### The Problem

The current MVP definition includes three major features: **historical data browsing**, **AI Q&A agent**, and **core mobile navigation**. Both the Lead and Backend Dev assess this as too ambitious for a first release. The AI agent alone roughly doubles backend effort and introduces significant unknowns (prompt engineering, safety validation, cost management, non-deterministic testing).

This blocks development because the team needs to know *what to build first* before they can plan sprints, estimate timelines, or start coding.

### Where Richard and Gilfoyle Agree

| Area | Consensus |
|------|-----------|
| AI can be cleanly deferred | The AI agent is architecturally additive — it reads from the same database but adds its own endpoints, services, and dependencies. No coupling to browsing features. |
| Data browsing is a real product | Seasons, races, results, standings, driver/constructor profiles — this is what most F1 reference apps offer, and people use them. |
| Auth should ship in MVP regardless | Needed for future AI rate limiting, personalization, and telemetry gating. It's foundational, not AI-specific. |
| AI doubles backend effort | Gilfoyle estimates 2-3 weeks for browsing API, +2-3 weeks to add AI on top. The AI testing surface is much larger due to non-deterministic LLM output. |
| Lay AI foundations now | Include the read-only DB role, database views for common queries, and schema documentation in Phase 1. These cost almost nothing and accelerate Phase 2. |

### Where They Differ (Nuance Only)

| Detail | Richard | Gilfoyle |
|--------|---------|----------|
| Phase 1 estimate | 4-6 weeks | 2-3 weeks (backend only; frontend adds time) |
| Phase 2 estimate | 2-3 weeks | 2-3 weeks |
| AI schema prep | Create PostgreSQL views now for common patterns | Write a schema-to-prompt generation script now |
| Include conversation tables in initial migration? | Not mentioned | Suggests yes (cheap to add), but not insistent |

These are tactical differences resolved during sprint planning.

### Options

#### Option A: Full MVP (Original Plan)

Ship data browsing + AI agent + navigation together as one release.

| Dimension | Assessment |
|-----------|------------|
| **What users get** | Complete experience: browse F1 data AND ask AI questions from day one. |
| **Timeline** | 6-9 weeks (backend: 4-6 weeks; frontend in parallel adds coordination overhead; AI prompt tuning often expands). |
| **Risk** | **High.** Three major features, non-deterministic AI testing, prompt engineering iteration, Azure OpenAI integration — all on the critical path for a single release. If any one area slips, everything slips. |
| **Market validation** | Tests the full vision, but if users don't engage, you won't know which part failed — the data, the AI, or the UX. |
| **Cost** | Azure OpenAI costs start from day one. Need monitoring and budget controls before any user touches the app. |

#### Option B: MVP-Lite + AI Fast-Follow (Team Recommendation)

**Phase 1 (MVP-Lite, 4-6 weeks):** Data browsing + core navigation + authentication.
**Phase 2 (AI Fast-Follow, 2-3 weeks after Phase 1):** AI agent with full safety rails.

| Dimension | Assessment |
|-----------|------------|
| **What users get — Phase 1** | Browse seasons, races, results, standings. View driver and constructor profiles with career stats. Dark/light mode. Full navigation. Account creation. A complete F1 reference app. |
| **What users get — Phase 2** | AI Q&A chat interface with natural language F1 queries. Conversation history. Suggested questions. All safety rails from Q18 decision. |
| **Timeline** | Phase 1: 4-6 weeks. Phase 2: 2-3 weeks after. Total: 6-9 weeks — same total, but Phase 1 ships earlier and is lower risk. |
| **Risk** | **Low-Medium.** Phase 1 is standard CRUD — well-understood, deterministic, testable. Phase 2 has the LLM unknowns, but by then the data layer is battle-tested. |
| **Market validation** | Phase 1 answers the foundational question: "Do F1 fans want a mobile historical data app?" If yes, Phase 2 is the upgrade. If no, you saved 2-3 weeks of AI work. |
| **Cost** | No Azure OpenAI spend until Phase 2. Real savings during MVP development. |

**Phase 1 includes these AI foundations (zero waste):**

| Foundation | Effort | Why now |
|------------|--------|---------|
| `werace_ai_readonly` PostgreSQL role | 10 minutes | One SQL statement in initial migration. Ready for Phase 2. |
| Database views for common query patterns | 1-2 days | Serves both browsing API and future AI agent. |
| Schema documentation | 0.5 days | Becomes the AI system prompt in Phase 2. |
| Auth infrastructure | 2-3 days | Required for AI rate limiting, but also for telemetry and future personalization. |
| Reserve `/ai/*` API namespace | 0 days | Document the intended contract so frontend can prepare a UI shell. |

#### Option C: Bare-Bones MVP (Browsing Only, No Auth)

Ship the absolute minimum: data browsing + navigation. No authentication, no AI, no personalization hooks.

| Dimension | Assessment |
|-----------|------------|
| **What users get** | Browse F1 data anonymously. No accounts, no AI, no personalized features. |
| **Timeline** | 2-4 weeks. Fastest possible release. |
| **Risk** | **Low** technically. But **high product risk**: without auth, there's no path to AI rate limiting, monetization, or personalization without a rearchitecture later. |
| **Market validation** | Tests the narrowest hypothesis: "Will anyone download and browse F1 data on their phone?" But doesn't test the differentiator (AI). |
| **Cost** | Minimal infrastructure. But auth added later requires retrofit — more expensive than building it in. |

### Product Perspective

The hardest hypothesis to validate is whether F1 fans want *another* mobile app for race data. The data browsing experience is the foundation — if nobody uses it, the AI agent doesn't matter. Shipping data browsing first lets us **learn from real users** before investing in AI.

But authentication is not optional scaffolding. It's the prerequisite for every future feature: AI rate limiting, conversation persistence, monetization, personalization. Cutting auth (Option C) creates technical debt that costs more to retrofit than to build correctly the first time.

**Option B** gives us a shippable product in 4-6 weeks, validates the core market hypothesis, preserves the full architecture vision, and positions us to ship AI 2-3 weeks later with a battle-tested data layer underneath it.

The counterargument — "WeRace is pitched as an AI-powered F1 app, so shipping without AI feels incomplete" — is valid. But a polished data app that works reliably is better than a rushed AI feature that hallucinates wrong race results. First impressions matter. We get one App Store launch.

### Team Recommendation

**Option B: MVP-Lite + AI Fast-Follow.**

Richard and Gilfoyle both independently recommend deferring the AI agent. Their reasoning is consistent: the AI agent is architecturally clean to separate, roughly doubles backend effort, and introduces non-deterministic testing challenges. Phase 1 delivers a real product. Phase 2 delivers the differentiator on a solid foundation.

### What Vincent Needs to Decide

| # | Decision | Options | Impact |
|---|----------|---------|--------|
| 1 | **Approve the two-phase approach?** | Yes (Option B) / No (Option A or C) | Determines sprint planning, timeline, and what ships first. |
| 2 | **If Option B: should Phase 1 include a visible "AI Coming Soon" teaser?** | Yes (builds anticipation, sets expectation) / No (cleaner launch, no unkept promises) | Affects frontend work and user messaging. |
| 3 | **If Option B: is 2-3 weeks between phases acceptable, or does AI need to ship within 1 week of data browsing?** | 2-3 weeks / Tighter window | Affects Phase 2 scope and pressure on prompt engineering quality. |
| 4 | **If Option A: accept the risk of a 6-9 week integrated timeline with higher delivery uncertainty?** | Yes / No | Only relevant if Vincent overrides the team recommendation. |

### Follow-Up Questions

- **F1:** Should we plan a private beta for Phase 1 (data browsing) before public launch, or go straight to App Store?
- **F2:** When AI ships in Phase 2, is it available to all authenticated users or gated behind a premium tier from day one?
- **F3:** Should the Phase 1 frontend include an empty "AI" tab or navigation element as a placeholder, or keep the UI clean until Phase 2?
- **F4:** What's the success metric for Phase 1? Daily active users? Session length? Specific screen engagement? This determines whether we greenlight Phase 2.
- **F5:** If Phase 1 market validation is weak (low engagement), do we still proceed with Phase 2 (AI), or pivot?

---

## Summary: Decisions Needed from Vincent

| # | Blocker | Decision | Team Recommendation |
|---|---------|----------|-------------------|
| 1 | Q18 | Approve defense-in-depth safety approach (Option A)? | **Yes** — unanimous |
| 2 | Q18 | Daily AI query limit per user (50 or 100)? | Start at 50, tune based on data |
| 3 | Q18 | Monthly Azure OpenAI budget ceiling? | Team needs a number to design the circuit breaker |
| 4 | Q18 | Allow prediction questions or historical-only? | Team leans historical-only for MVP |
| 5 | Q1 | Approve two-phase MVP approach (Option B)? | **Yes** — unanimous |
| 6 | Q1 | "AI Coming Soon" teaser in Phase 1? | Product call — team has no strong opinion |
| 7 | Q1 | Acceptable gap between Phase 1 and Phase 2? | 2-3 weeks recommended |

**Next step:** Vincent decides on items 1-7. Once confirmed, the team can begin sprint planning for Phase 1.
