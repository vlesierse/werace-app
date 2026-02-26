# Blocker Brainstorm — Richard (Lead / Architect)

**Date:** 2026-02-26
**For:** Monica (Product Owner) — synthesis into proposals for Vincent
**Re:** Q18 (AI Safety Rails) and Q1 (MVP Scope Assessment)

---

## Blocker 1: Q18 — AI Safety Rails

### Architecture Options for Safe LLM-Generated SQL

I see five layers of defense. We should not pick one — we should stack them. Defense in depth.

| Layer | Mechanism | What It Prevents |
|-------|-----------|-----------------|
| **1. Read-only DB user** | Dedicated PostgreSQL role with `SELECT`-only grants on specific tables/views | DDL, DML (INSERT/UPDATE/DELETE), privilege escalation |
| **2. Schema-aware prompt engineering** | System prompt includes exact schema (table names, column names, types, relationships). Instructs LLM: "You may only SELECT from these tables. Never use INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, or any DDL." | Reduces probability of harmful SQL generation at source |
| **3. Query validation middleware** | .NET middleware that parses the generated SQL (using a SQL parser like `SqlParser` for .NET or regex-based blocklist) before execution. Rejects anything that isn't a single SELECT statement. | Catches anything the LLM hallucinates past the prompt constraints |
| **4. Query allowlist / template approach** | Define a set of parameterized query templates (e.g., "race winner by season+round", "driver career stats", "championship standings at round N"). LLM selects template + fills parameters instead of generating free-form SQL. | Eliminates free-form SQL entirely — zero injection surface |
| **5. Row/time limits on execution** | PostgreSQL `statement_timeout` (e.g., 5 seconds), `LIMIT` injection if missing, connection pooling with dedicated pool for AI queries | Runaway queries, accidental full table scans, resource exhaustion |

### My Recommendation

**Implement layers 1 + 2 + 3 + 5 for MVP. Evaluate layer 4 as a future tightening.**

Here's why:

- **Layer 1 (read-only user) is non-negotiable.** Zero effort, maximum downside protection. The AI's database connection must use a role that literally cannot modify data. This is a 10-minute PostgreSQL configuration. No excuse to skip it.

- **Layer 2 (schema-aware prompts) is our primary control.** Azure OpenAI's system prompts are reliable for instruction following. We feed the exact schema, constrain to SELECT-only, and include examples. This handles 95%+ of cases correctly.

- **Layer 3 (query validation) is our safety net.** Parse the generated SQL in .NET before executing. At minimum: reject if it contains multiple statements (`;` splitting), reject if it's not a SELECT, reject if it references tables outside the allowed set. A simple AST-level check. Libraries exist for this in .NET. This catches the edge cases where the LLM ignores prompt instructions.

- **Layer 5 (execution limits) prevents operational disasters.** A `statement_timeout` of 5 seconds on the AI query pool. Force a `LIMIT 1000` if the LLM omits it. This protects against queries like `SELECT * FROM lap_times` (millions of rows).

- **Layer 4 (strict allowlist/templates) is the safest option but I'd defer it.** It eliminates the SQL injection surface entirely, but it also kills the magic of natural language querying. Users would only be able to ask questions we've pre-anticipated. For MVP, the combination of read-only user + prompt engineering + validation middleware gives us strong safety with full flexibility. If we see abuse patterns post-launch, we can tighten to templates.

**Implementation in our stack:**

- .NET Minimal API: Add an `AiQueryValidationMiddleware` or a service class (`SqlSafetyValidator`) that runs before any generated SQL hits the database.
- PostgreSQL: Create a `werace_ai_reader` role with `SELECT` on specific tables/views. Use a separate connection string in Aspire for the AI query path.
- Azure OpenAI: System prompt includes full schema DDL (table definitions) and strict instructions. We version-control the system prompt alongside the code.

### Rate Limiting Architecture

Rate limiting should live at **two layers**:

1. **API middleware (.NET):** Per-user rate limiting on `POST /ai/query` using a sliding window counter. Store counters in Redis (we already have Redis in the stack for caching). This is the primary enforcement point.
   - **Suggested limits:** 50 queries/day per user, 10 queries/minute burst. Configurable via app settings.
   - **Why middleware, not API gateway:** We need per-authenticated-user limits, not per-IP. The middleware has access to the user identity from the JWT. An API gateway would only see IP addresses (and many users share IPs behind NATs/corporate networks).

2. **Azure OpenAI budget controls:** Set a monthly token budget and per-request max token limits in the Azure OpenAI configuration. This is the cost ceiling — even if our app-level rate limiting has a bug, Azure won't let us blow the budget.

**Do NOT rely on database-level rate limiting.** By the time a query hits PostgreSQL, the expensive part (the LLM call to Azure OpenAI) has already happened. Rate limiting must happen before the LLM call.

### Content Boundaries

The AI agent should refuse:

| Category | Examples | Enforcement |
|----------|----------|-------------|
| **Non-F1 topics** | Politics, personal advice, general knowledge | System prompt: "You are an F1 data assistant. Refuse any question not related to Formula 1." |
| **Speculation / predictions** | "Who will win the 2026 championship?" | System prompt: "Only answer based on historical data in the database. Do not speculate or predict." |
| **Personal data about drivers** | "What's Hamilton's home address?" | System prompt + schema constraint (we don't store personal data, so the DB won't have it) |
| **Harmful content** | Anything violent, illegal, etc. | Azure OpenAI built-in content filters (enabled by default) |
| **Real-time data (MVP)** | "What's happening in the race right now?" | System prompt: "You have access to historical data only. You cannot provide live or real-time information." |

**Enforcement approach:** Layered — Azure OpenAI content filters (automatic) + system prompt instructions (soft boundary) + response validation in .NET (check for refusal patterns, flag suspicious responses for logging).

### Safety vs. Capability Trade-off

My position: **Lean toward capability for MVP, with strong guardrails.** The AI agent is a differentiator for WeRace. If we over-restrict it (strict template allowlist), it becomes a glorified search box. Users should be able to ask surprising, creative questions — "Which wet-weather races had the most overtakes?" — and get answers. The read-only user + validation middleware protects us from actual harm. The risk we're accepting is that the LLM sometimes generates inefficient or incorrect SQL, which we mitigate with execution limits and confidence scoring.

---

## Blocker 2: Q1 — MVP Scope Assessment

### Technical Dependency Analysis

**The AI agent can be cleanly separated from data browsing.** Here's the dependency map:

```
Data Browsing (standalone)          AI Agent (depends on data layer)
├── Seasons list                    ├── Uses same PostgreSQL schema
├── Race details                    ├── Uses same data tables
├── Driver/Constructor profiles     ├── Adds: Azure OpenAI integration
├── Standings                       ├── Adds: Conversation model (users ↔ messages)
└── Core navigation                 ├── Adds: SqlSafetyValidator service
                                    ├── Adds: AI-specific rate limiting
                                    └── Adds: /ai/* API endpoints
```

**Key insight:** The AI agent is an additive feature that sits on top of the data layer. It does not change the schema for data browsing. It does not change the REST API contracts for browsing endpoints. It adds its own endpoints (`/ai/query`, `/ai/conversations`), its own services, and its own infrastructure dependency (Azure OpenAI).

The only shared dependency is the PostgreSQL schema — which we need for data browsing anyway. The AI agent reads from the same tables. It does not require additional tables for the core F1 data (only for conversation persistence, which is AI-specific).

**Verdict: Clean separation. No coupling concerns.**

### Architecture Impact of Deferring

If we defer the AI agent to a fast-follow, the impact on current architecture is **minimal but we should be intentional about it**:

| Aspect | Impact if AI deferred | Action now |
|--------|----------------------|------------|
| **Database schema** | No change. Same tables for browsing and future AI. | Design schema as planned. No AI-specific schema needed for browsing MVP. |
| **API contracts** | Drop `/ai/*` endpoints from MVP. All `/seasons`, `/races`, `/drivers` endpoints unchanged. | Design browsing API contracts. Reserve `/ai/*` namespace in docs. |
| **Authentication** | Still needed for future AI rate limiting + conversation persistence. But also needed for P2 personalization (favorites, bookmarks). | **Implement auth in MVP regardless.** It's foundational. Anonymous browsing + optional auth for future features. |
| **Redis** | Still useful for caching browsing data. | Keep Redis in stack. |
| **Azure OpenAI** | Not needed for MVP. Don't provision until AI sprint. | Skip Azure OpenAI setup. Saves infrastructure cost during MVP. |
| **System prompt / AI service** | Not needed for MVP. | Don't build it yet. But document the intended approach (this brainstorm doc) so the architecture is ready. |

**One thing to get right now regardless:** The database views. If we create well-structured PostgreSQL views for common query patterns (driver career stats, race results with positions, championship standings at a given round), those views serve both the browsing API and the future AI agent. The AI agent will generate `SELECT` queries against these views. Design the views now, even if the AI consumer comes later.

### My Recommendation

**Defer AI agent from MVP. Ship MVP-lite (data browsing + navigation + auth). AI agent as immediate fast-follow.**

Rationale:

1. **Risk reduction.** Three major features in MVP (browsing + AI + navigation) is genuinely risky. The AI agent has the most unknowns: prompt engineering iteration, safety validation, Azure OpenAI integration, conversation UX. Each of those is a rabbit hole.

2. **The AI agent isn't the riskiest thing to validate first.** The riskiest hypothesis for WeRace is: "Do F1 fans want a mobile app for historical data?" If no one browses the data, the AI agent doesn't matter. Ship the browsing experience, validate engagement, then layer AI on top.

3. **MVP-lite is a real product.** Data browsing + standings + driver profiles + season history — that's a useful app. It's not a hollow shell. It's what most F1 reference apps are, and people use them.

4. **AI benefits from a baked data layer.** If we ship browsing first, we'll discover schema issues, missing data, edge cases (sprint races, DNS/DNF handling, shared drives, etc.) before the AI agent tries to query them. The AI agent will be better if it's built on a battle-tested data layer.

5. **Cost deferral.** No Azure OpenAI spend until we're ready. Real savings during the MVP phase.

### Phasing — The Cleanest Cut

**Phase 1 (MVP — 4-6 weeks):**
- Data browsing: Seasons, races, results, standings, driver/constructor profiles
- Core navigation: Tab bar, season browser, race detail drill-down, dark/light mode
- Authentication: .NET Identity with email/password + passkeys, anonymous browsing
- Data pipeline: Jolpica dump import to PostgreSQL
- API: All browsing endpoints (`/seasons`, `/races`, `/drivers`, `/constructors`, `/standings`)
- Infrastructure: .NET 10 Minimal API + PostgreSQL + Redis + Aspire

**Phase 2 (AI Fast-Follow — 2-3 weeks after MVP):**
- AI agent: Azure OpenAI integration, `POST /ai/query`, conversation persistence
- Safety rails: Read-only DB user, SqlSafetyValidator, rate limiting
- AI UX: Chat interface, suggested questions, conversation history
- API: `/ai/query`, `/ai/conversations`

**Foundations to lay in Phase 1 (even though AI is deferred):**
1. **Database views** for common query patterns (career stats, race results, standings). These serve both browsing and future AI.
2. **Authentication infrastructure** — needed for AI rate limiting later, but also useful for future personalization.
3. **Redis** — already in stack for caching. Will also store AI rate limit counters in Phase 2.
4. **Schema documentation** — document all tables, columns, relationships. This becomes the AI agent's system prompt schema in Phase 2.
5. **Reserve `/ai/*` API namespace** — don't implement, but document the intended contract so frontend can prepare the UI shell.

---

## Summary of Positions

| Topic | Richard's Position |
|-------|-------------------|
| SQL safety | Defense in depth: read-only DB user + schema-aware prompts + validation middleware + execution limits. No strict allowlist for MVP. |
| Rate limiting | API middleware (Redis-backed, per-user) + Azure OpenAI budget caps. Not at DB level. |
| Content boundaries | Layered: Azure content filters + system prompt + response validation. F1-only, historical-only, no speculation. |
| MVP scope | Defer AI. Ship data browsing + nav + auth. AI as 2-3 week fast-follow. |
| Phase 1 foundations | Build DB views, auth, Redis, schema docs — all serve Phase 2 with zero waste. |
| Coupling risk | None. AI agent is additive, not entangled. Clean separation confirmed. |
