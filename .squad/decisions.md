# Decisions

> Team decisions that all agents must respect. Maintained by Scribe.

## Team & Stack (2026-02-23)

### Team formation
**By:** Squad (Coordinator)
**What:** Team formed with Silicon Valley casting. Richard (Lead), Dinesh (Frontend), Gilfoyle (Backend), Jared (Tester).
**Why:** Project kickoff — Formula 1 mobile app with React Native + .NET 10 Minimal API + Aspire.

### Tech stack
**By:** Vincent Lesierse (Owner)
**What:** React Native frontend with popular design framework, .NET 10 Minimal API backend, Aspire for local development orchestration, AI agent for F1 Q&A.
**Why:** Owner's technology choice for the WeRace app.

## PRD Decisions (2026-02-23)

### Architectural Decisions

**1. Database: PostgreSQL**
**By:** Richard (Lead)  
**What:** Use PostgreSQL as primary database for historical F1 data  
**Why:** Relational model fits F1 data structure (seasons → races → results), strong query capabilities needed for AI agent, proven scalability  
**Alternatives Considered:** MongoDB (rejected: F1 data is highly relational), CosmosDB (rejected: cost)

**2. UI Framework: React Native Paper**
**By:** Richard (Lead)  
**What:** Use React Native Paper as design framework  
**Why:** Most popular React Native design library, Material Design 3 compliant, excellent theming (dark/light mode), comprehensive component set, active maintenance  
**Alternatives Considered:** React Native Elements (less Material-focused), custom components (too much overhead)

**3. API Architecture: RESTful with Resource-Based Endpoints**
**By:** Richard (Lead)  
**What:** RESTful API with endpoints grouped by resource (/seasons, /races, /drivers, /constructors, /telemetry, /ai)  
**Why:** Mobile-friendly, predictable structure, easy to cache, aligns with React Native's fetch/axios patterns  
**Alternatives Considered:** GraphQL (rejected: adds complexity for mobile, over-fetching not a major concern with REST + good endpoint design)

**4. AI Agent Approach: RAG with SQL Query Generation**
**By:** Richard (Lead)  
**What:** Retrieval-Augmented Generation using LLM to generate SQL queries from natural language, then format results into answers  
**Why:** Structured F1 data in PostgreSQL is queryable via SQL, more accurate than pure LLM hallucination, easier to verify/debug  
**Alternatives Considered:** Vector search only (rejected for MVP: F1 data is structured), pure LLM without retrieval (rejected: accuracy issues)

### Feature Prioritization

**P0 (MVP) Scope — Two-Phase Approach**

*Phase 1 — MVP (4-6 weeks):*
- Historical race data browsing (seasons, races, results, standings)
- Core mobile navigation with dark/light mode
- Driver/team profiles with career stats
- Authentication (.NET Identity, email/password + passkeys)
- AI foundations (read-only DB role, database views, schema docs)

*Phase 2 — AI Fast-Follow (2-3 weeks after Phase 1):*
- AI Q&A agent for F1 questions (with defense-in-depth safety architecture)

**P1 (Post-MVP) Scope**
- Race weekend companion (schedule, session results, push notifications)
- Telemetry data exploration (lap traces, comparative views)
- Enhanced AI agent (multi-turn conversations, telemetry queries)

**P2 (Future) Scope**
- Personalization (favorites, bookmarks, watch history)
- Circuit explorer (3D maps, corner analysis)
- Offline mode

### Open Questions Requiring Team Input

**1. AI Backend Provider**  
Azure OpenAI vs. self-hosted model (e.g., Llama 3)  
*Recommendation:* Start with Azure OpenAI for MVP, evaluate self-hosted post-launch if costs scale

**2. Data Licensing**  
Legal rights to use Ergast API / OpenF1 API data in commercial app  
*Risk:* May require direct licensing from FOM (Formula One Management)  
*Action:* Legal review before public beta

**3. Telemetry Data Source**  
Where to source lap-by-lap telemetry for P1 features  
*Recommendation:* Start with OpenF1 + FastF1 for P1, evaluate official feed if user demand justifies cost

**4. Monetization Model**  
Free with ads, freemium, or paid upfront  
*Recommendation:* Freemium (free historical data + basic AI, premium telemetry for $2.99/month) balances accessibility with revenue

## PRD Review Findings (2026-02-23)

### PRD Review Completed by Monica (Product Owner)

**What:** Comprehensive review of `docs/PRD.md` v1.0 produced `docs/PRD-REVIEW.md` with 36 stakeholder questions, 8 flagged vague requirements, and 10 recommendations.

**Status:** 🟡 PRD requires stakeholder clarification before development can begin

### Critical Blockers (Must Resolve Before Development Starts)

**Q25 — Authentication Model** ✅ RESOLVED
- **Resolution:** .NET Identity with two authentication options:
  - **Email/Password:** Standard forms-based authentication
  - **Passkeys:** FIDO2/WebAuthn support for passwordless access
  - **Anonymous Access:** Allowed for browsing races and cached historical data
  - **Authenticated Features:** AI agent queries and telemetry data access require login
- **Impact:** Unblocks rate limiting, conversation persistence, feature gating, and monetization architecture.
- **Documented in:** `docs/PRD.md` § Authentication & Authorization
- **Follow-up Items Requiring Stakeholder Input:**
  - **F1 — Apple Sign-In Requirement:** Do we need Apple Sign-In as third option for App Store compliance?
  - **F2 — Anonymous Rate Limiting:** Need IP-based or device-fingerprint rate limiting strategy for anonymous users
  - **F3 — Login UX Flow:** Define transition UX when anonymous user accesses gated features (modal vs. redirect)
  - **F4 — Passkey Mobile Implementation:** Assess feasibility of FIDO2/WebAuthn in React Native (iOS Passkeys API, Android Credential Manager)
  - **F5 — Session Duration:** Define token lifetime and refresh strategy for mobile UX (indefinite vs. inactivity expiry)
  - **F6 — User Data Model:** Do we need custom User entity to link conversations, favorites, and usage tracking?
  - **F7 — AI Rate Limits:** Define per-user daily/hourly query limits and token budgets for cost control

**Q12 — Data Source (Ergast API Deprecated)** ✅ RESOLVED
- **Resolution:** Jolpica API (Ergast-compatible) replaces Ergast as primary historical data source.
  - **Jolpica API:** Drop-in replacement for deprecated Ergast API, compatible interface
  - **Database Dumps:** Jolpica exposes database dump files for direct PostgreSQL import
  - **Pipeline Approach:** Bulk import from dump files rather than incremental API scraping
- **Impact:** Unblocks data pipeline design. Simplifies initial data seeding (dump import vs. ETL).
- **Documented in:** `docs/PRD.md` § Data Sources
- **Follow-up Items Requiring Stakeholder Input:**
  - **F1 — Data Freshness:** What is Jolpica's update frequency? How quickly are new race results available in dump files vs. API?
  - **F2 — Dump Format Compatibility:** What format are the dump files (SQL, CSV, custom)? Do they map directly to our PostgreSQL schema or require transformation?
  - **F3 — Data Completeness:** Does Jolpica cover the same data range as original Ergast (1950–2024)? Any gaps or additions?
  - **F4 — Licensing Terms:** What are Jolpica's licensing terms for commercial use? Any attribution or usage restrictions?
  - **F5 — Sync Strategy:** One-time dump import + delta updates via API, or periodic full re-import? What triggers a sync?
  - **F6 — 2025+ Data:** Does Jolpica cover current/future seasons, or do we need OpenF1 for recent data and Jolpica for historical only?

### Open Follow-Ups: Data Source (Q12) — Filed 2026-02-26

**By:** Monica (Product Owner)
**Context:** Vincent confirmed Jolpica API as replacement for Ergast. These sub-questions require further stakeholder input before data pipeline design can proceed.

| ID | Question | Priority |
|----|----------|----------|
| F1 | **Data Freshness:** How frequently does Jolpica update? How quickly after a race are results in API vs. dump files? | Medium |
| F2 | **Dump Format Compatibility:** What format are Jolpica dump files (SQL, CSV, MySQL)? Direct PostgreSQL mapping or transformation needed? | High |
| F3 | **Data Completeness:** Does Jolpica cover full 1950–present range? Any gaps vs. original Ergast? | Medium |
| F4 | **Licensing Terms:** Commercial use permitted? Attribution or rate limit restrictions? | High |
| F5 | **Sync Strategy:** One-time dump + API deltas, or periodic full re-import? Affects pipeline architecture. | High |
| F6 | **2025+ Coverage:** Does Jolpica cover current/future seasons or only historical? Do we need OpenF1 for recent data? | Medium |

**Action:** Vincent to confirm F1–F6. Most critical for pipeline: F2, F5. Most critical for legal: F4.

**Q18 — AI Safety Rails** ✅ RESOLVED
- **Resolution:** Defense-in-depth safety architecture (Option A — team recommendation accepted).
  - **4-layer safety stack:** Read-only DB role (`werace_ai_readonly`) → schema-aware LLM prompts → SQL validation middleware → execution limits (`statement_timeout` 5s, `LIMIT 1000`)
  - **Rate limiting:** 50 queries/day per authenticated user (tunable based on data)
  - **Content scope:** Historical F1 data only — no predictions, no personal data, no non-F1 topics
  - **Budget:** Azure OpenAI monthly ceiling TBD during Phase 2 planning
- **Impact:** Full natural language flexibility preserved while eliminating SQL injection surface. Implementation in Phase 2.
- **Documented in:** `docs/PRD.md` § AI Safety Architecture

**Q1 — MVP Scope Assessment** ✅ RESOLVED
- **Resolution:** MVP-Lite + AI Fast-Follow (Option B — team recommendation accepted).
  - **Phase 1 (4-6 weeks):** Historical data browsing + core navigation + authentication + AI foundations
  - **Phase 2 (2-3 weeks after Phase 1):** AI-powered Q&A agent with defense-in-depth safety rails
  - **"AI Coming Soon" teaser:** Deferred to team decision during Phase 1 planning
  - **Phase gap:** 2-3 weeks acceptable
- **Impact:** Reduces MVP risk by shipping deterministic CRUD first. Auth in Phase 1 enables Phase 2 rate limiting. AI foundations laid in Phase 1 for seamless Phase 2 start.
- **Documented in:** `docs/PRD.md` § Core Features

### Additional Priority Questions

**Data Model Gaps:**
- Q3: Sprint races and Sprint Shootouts (2021+) not in data model
- Q4: Qualifying results not modeled (stored in Result table with flag, or separate entity?)
- Q5: Pit stop data not modeled (in scope for MVP?)

**User Experience:**
- Q7: Empty/error state definitions missing (no internet, no data, race not happened, off-season)
- Q9: Off-season Home Screen design missing (what shows when no upcoming race?)
- Q10: Pagination strategy undefined for lists (800+ drivers, 1000+ races)

**Non-Functional Requirements:**
- Q25: Accessibility WCAG conformance level not specified (WCAG 2.1 AA is mobile standard)
- Q26: iOS/Android version targets not specified (affects library compatibility)
- Q30: Concurrent user targets missing (affects infrastructure decisions for AI agent)

**Business & Legal:**
- Q33: "WeRace" trademark clearance not confirmed
- Q35: Privacy policy missing (App Store requirement, affects data retention)
- Q36: App store category/ratings not finalized

### Vague Requirements Flagged (8)

| Requirement | Issue | What's Needed |
|---|---|---|
| "Smooth scrolling" | Not measurable | Frame rate target (60fps?) |
| "Instant feedback" | Not measurable | Millisecond threshold (< 100ms?) |
| "High contrast ratios" | Not specific | WCAG AA min (4.5:1 text, 3:1 large) |
| "Scalable fonts" | Not specific | Dynamic type support definition |
| "Speed" (design principle) | Entire principle vague | FCP, TTI, navigation time targets |
| "Comprehensive database" | Undefined | Season coverage, minimum data per race |
| "Understandable telemetry" | Vague | Target user F1 knowledge level |
| "Concise answers" | No spec | Max words? Sentence? Paragraph? |

### 10 Recommendations (Prioritized)

**Before Development (4 items):**
1. Resolve authentication model (Q25) → architectural foundation
2. Confirm data source (Q12) → can't design pipeline without it
3. Define AI safety rails (Q18, Q20, Q22) → security + cost control required
4. Scope MVP tighter (Q1) → reduce delivery risk with phased approach

**Before Design (3 items):**
5. Define empty/error states (Q7, Q9, Q29) → every screen needs these UX decisions
6. Resolve content dependencies (Q11, Q17) → circuit images, driver photos sourcing
7. Clarify pagination (Q10) → affects every list view in the app

**Before Beta (3 items):**
8. Legal review (Q33, Q35, Q36) → trademark, privacy, licensing are launch blockers
9. Analytics instrumentation (Q34) → can't measure success metrics without it
10. Accessibility conformance (Q27) → WCAG 2.1 AA is standard for mobile apps

**Action:** ~~Vincent must respond to all 36 questions in `docs/PRD-REVIEW.md` (numbered for easy reference). Critical path: Q1, Q12, Q18, Q25.~~ All 4 critical blockers now resolved.

## Phase 1 Plan Decisions (2026-02-26)

**By:** Richard (Lead / Architect)
**Document:** `docs/PHASE1-PLAN.md`
**Status:** 🟢 Ready for execution

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **5 sprints × 1 week** | 5 weeks balances the 4–6 week window. 1 sprint buffer over minimum. |
| 2 | **6 epics: Scaffolding, Data Pipeline, API, Auth, Mobile UI, AI Foundations** | Clean separation. Each epic has single primary owner. Dependencies flow left-to-right. |
| 3 | **Offset-based pagination, pageSize=20, max 100** | Simpler than cursor for static historical data. Revisit for Phase 2 live data. |
| 4 | **Consistent JSON envelope: `{ "data", "pagination", "error" }`** | Every endpoint same shape. Errors are structured, not strings. |
| 5 | **Client-side search for Phase 1** | Driver/constructor/circuit lists < 1000 items. Evaluate backend search in Phase 2. |
| 6 | **Mock data decoupling** | Dinesh builds UI against API contracts with JSON fixtures. Prevents Gilfoyle bottleneck. |
| 7 | **Passkeys as best-effort** | FIDO2 in React Native uncertain. Email/password satisfies Phase 1 requirements. Backend supports passkeys regardless. |
| 8 | **5 database views as AI foundations** | `v_driver_career_stats`, `v_constructor_season_stats`, `v_race_summary`, `v_head_to_head`, `v_circuit_records`. Zero cost now, immediate Phase 2 value. |
| 9 | **Redis for caching AND per-user request tracking** | Single Redis instance dual-purpose. Historical data 24h, current season 1h. Per-user counters for Phase 2 rate limiting. |
| 10 | **Richard as fallback for API endpoints** | Lead picks up simpler endpoints if Gilfoyle bottleneck hits. |

## Data Model Decisions (2026-02-26)

**By:** Gilfoyle (Backend Developer)
**Document:** `docs/TECHNICAL-FOUNDATION.md`
**Status:** Proposals — accepted into Phase 1 plan

### Q3 — Sprint Races: Separate `sprint_results` Table

**Decision:** Model sprint races as a separate `sprint_results` table mirroring the `results` table structure.
**Rationale:** Aligns with Jolpica dump format (near-zero transformation). Clean API surface (`/races/{id}/sprint`). Sprint format keeps changing (2021–present) — separate table isolates volatility. No `session_type` filter needed on race results queries.

### Q4 — Qualifying: Separate `qualifying` Table

**Decision:** Qualifying is a separate entity with Q1/Q2/Q3 time columns.
**Rationale:** Different data shape than race results (Q1/Q2/Q3 times vs. position/laps/time). Jolpica stores qualifying separately. Historical format variation (single-lap, aggregate, knockout) handled by nullable columns. API maps directly: `GET /races/{id}/qualifying`.

### Q5 — Pit Stops: Included in Phase 1

**Decision:** Include `pit_stops` table and `GET /races/{id}/pit-stops` endpoint in Phase 1.
**Rationale:** Zero additional development cost (Jolpica dump includes pit stops). High browsing value for race strategy. Enables best AI queries in Phase 2. Data available 2012–present. ~2 hours of work for high return.

## Blocker Proposals — Q18 & Q1 (2026-02-26)

### Q18 — AI Safety Rails: Team Proposals

**Status:** ✅ RESOLVED — Vincent approved Option A (Defense-in-Depth)

**Context:** The AI agent generates SQL from natural language with no security guardrails specified. Richard and Gilfoyle independently brainstormed architectural and implementation approaches. Monica synthesized into 3 options.

**Team Consensus:**
- Read-only DB user (`werace_ai_readonly`) is non-negotiable
- Schema-aware LLM prompts constrained to SELECT-only on F1 data tables
- Query validation middleware parses SQL before execution (reject non-SELECT, enforce table allowlist)
- Execution limits: `statement_timeout` (5s), forced `LIMIT 1000`, dedicated connection pool
- Rate limiting at API level before LLM call (not at DB level)
- Content boundaries: F1-only, historical-only, no predictions, no personal data
- Defer strict query template/allowlist — preserves natural language flexibility

**Options Presented to Vincent:**

| Option | Approach | Security | UX | Effort |
|--------|----------|----------|-----|--------|
| **A (Recommended)** | Defense-in-depth: 4 stacked safety layers | Strong — any single failure caught by next layer | Full natural language flexibility | ~3-4 days for validation pipeline |
| **B** | Strict template/allowlist — LLM picks from pre-approved queries | Maximum — zero injection surface | Significantly limited — only pre-anticipated questions work | Similar effort + ongoing template maintenance |
| **C** | Minimal — read-only user + prompts only, no validation middleware | Adequate floor but no defense-in-depth | Full flexibility | Saves 2-3 days vs. A |

**Decisions Required from Vincent:**

| # | Decision | Options |
|---|----------|---------|
| 1 | Approve defense-in-depth safety approach (Option A)? | Yes / No (B or C instead) |
| 2 | Daily AI query limit per user? | Richard: 50/day, Gilfoyle: ~100/day |
| 3 | Monthly Azure OpenAI budget ceiling? | ~$0.01/query; 1K users × 20 queries/day ≈ $6K/month |
| 4 | Allow prediction questions or historical-only? | Team leans historical-only for MVP |

**Full proposals:** `.squad/decisions/inbox/monica-blocker-proposals.md`
**Technical details:** `.squad/decisions/inbox/richard-blocker-brainstorm.md`, `.squad/decisions/inbox/gilfoyle-blocker-brainstorm.md`

### Q1 — MVP Scope: Team Proposals

**Status:** ✅ RESOLVED — Vincent approved Option B (MVP-Lite + AI Fast-Follow)

**Context:** Current MVP includes data browsing + AI agent + navigation. Both Richard and Gilfoyle assess this as too ambitious. AI doubles backend effort (2-3 weeks → 4-6 weeks) and introduces non-deterministic testing challenges. The AI agent is architecturally additive — clean separation confirmed.

**Team Consensus:**
- AI can be cleanly deferred — no coupling to browsing features
- Data browsing alone is a real, shippable product
- Auth must ship in MVP regardless (needed for AI rate limiting, personalization, monetization)
- AI roughly doubles backend effort with significantly more unknowns
- Lay AI foundations in Phase 1: read-only DB role, database views, schema docs

**Options Presented to Vincent:**

| Option | Approach | Timeline | Risk |
|--------|----------|----------|------|
| **A** | Full MVP (browsing + AI + nav together) | 6-9 weeks | High — AI unknowns on critical path |
| **B (Recommended)** | MVP-Lite Phase 1 (browsing + nav + auth), AI Fast-Follow Phase 2 | Phase 1: 4-6 weeks, Phase 2: 2-3 weeks | Low-Medium — Phase 1 is deterministic CRUD |
| **C** | Bare-bones (browsing only, no auth) | 2-4 weeks | Low tech risk, high product risk — no path to AI/monetization without retrofit |

**Decisions Required from Vincent:**

| # | Decision | Options |
|---|----------|---------|
| 5 | Approve two-phase approach (Option B)? | Yes / No (A or C instead) |
| 6 | Include "AI Coming Soon" teaser in Phase 1? | Yes (builds anticipation) / No (cleaner launch) |
| 7 | Acceptable gap between Phase 1 and Phase 2? | 2-3 weeks recommended |

**Full proposals:** `.squad/decisions/inbox/monica-blocker-proposals.md`

## E1 Project Scaffolding Decisions (2026-02-26)

### Backend Scaffolding (Gilfoyle)

**1. Aspire 13.1.2 via NuGet (not workload)**
**By:** Gilfoyle (Backend Developer)
**What:** Aspire consumed as NuGet packages. Templates installed via `dotnet new install Aspire.ProjectTemplates`.
**Why:** The Aspire workload is deprecated in .NET 10. All functionality comes from `Aspire.Hosting.*` and `Aspire.*` NuGet packages.

**2. Solution format: `.slnx`**
**By:** Gilfoyle (Backend Developer)
**What:** Solution file is `WeRace.slnx`, not `.sln`.
**Why:** `.slnx` is the new default format in .NET 10. Simpler XML, smaller diff surface.

**3. AppHost `WaitFor()` dependency wiring**
**By:** Gilfoyle (Backend Developer)
**What:** AppHost uses `WaitFor()` for PostgreSQL and Redis before starting the API.
**Why:** Prevents API from starting before dependencies are healthy. Aspire handles container lifecycle.

**4. Health checks auto-registered by Aspire components**
**By:** Gilfoyle (Backend Developer)
**What:** `/health` and `/alive` endpoints provided by ServiceDefaults. Aspire component packages auto-register health checks.
**Why:** Zero additional health check code. PostgreSQL and Redis connectivity verified automatically.

### Frontend Scaffolding (Dinesh)

**5. Expo SDK 55 managed workflow**
**By:** Dinesh (Frontend Developer)
**What:** Expo managed workflow with SDK 55, React 19.2, React Native 0.83.2.
**Why:** Latest stable Expo SDK. Managed workflow keeps the build pipeline simple.

**6. Separate Paper and Navigation themes**
**By:** Dinesh (Frontend Developer)
**What:** PaperProvider and NavigationContainer receive separate theme objects. `adaptNavigationTheme()` bridges colors.
**Why:** Merging Paper (MD3Typescale) and Navigation (FontStyle) fonts into one object causes TypeScript type conflicts.

**7. Paper `BottomNavigation.Bar` as custom tab bar**
**By:** Dinesh (Frontend Developer)
**What:** React Navigation's `createBottomTabNavigator` uses Paper's `BottomNavigation.Bar` via the `tabBar` prop.
**Why:** Full MD3 styling (active indicators, ripple, theming) while keeping React Navigation routing. Recommended by Paper docs.

**8. ThemeContext with AsyncStorage persistence**
**By:** Dinesh (Frontend Developer)
**What:** Theme preference (system/light/dark) stored in AsyncStorage, exposed via React Context.
**Why:** Users expect theme choice to survive app restarts. System-default is the fallback.

**9. `@expo/vector-icons` for icons**
**By:** Dinesh (Frontend Developer)
**What:** Using `MaterialCommunityIcons` from `@expo/vector-icons`.
**Why:** Bundled with Expo (no native linking), matches Paper's design language.

### Test Infrastructure (Jared)

**10. Test project at `tests/` (not `src/api/`)**
**By:** Jared (Tester)
**What:** Tests live at `tests/WeRace.Api.Tests/`, matching `docs/TECHNICAL-FOUNDATION.md` solution structure.
**Why:** Keeps source and test code separated at the top level.

**11. WebApplicationFactory for backend integration tests**
**By:** Jared (Tester)
**What:** Backend tests use `WebApplicationFactory<Program>` for in-memory HTTP integration testing.
**Why:** Real HTTP testing without external infrastructure.

**12. xUnit + FluentAssertions + coverlet**
**By:** Jared (Tester)
**What:** Standard .NET test stack with readable assertions and cross-platform coverage.
**Why:** Industry standard. FluentAssertions readable syntax. Coverlet for CI coverage reports.

**13. Test naming conventions**
**By:** Jared (Tester)
**What:** Backend: `MethodName_StateUnderTest_ExpectedBehavior`. Frontend: `it('should [behavior] when [condition]')`.
**Why:** Consistent naming across team makes tests self-documenting.