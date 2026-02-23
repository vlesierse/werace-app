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

**P0 (MVP) Scope**
- Historical race data browsing (seasons, races, results, standings)
- AI Q&A agent for F1 questions
- Core mobile navigation with dark/light mode
- Driver/team profiles with career stats

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
