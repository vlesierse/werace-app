# Product Requirements Document: WeRace

**Version:** 1.0  
**Last Updated:** 2026-02-23  
**Owner:** Vincent Lesierse  
**Lead:** Richard

---

## Overview

WeRace is a mobile application for Formula 1 fans that provides comprehensive access to historical F1 data, live race information, and an AI-powered Q&A agent. The app enables users to explore decades of F1 history, track current race weekends, and get instant answers to questions about races, drivers, teams, and telemetry data through natural language queries.

**Target Platform:** iOS and Android (React Native)  
**Primary Use Cases:** Pre-race research, live race companion, post-race analysis, historical data exploration

---

## Goals & Non-Goals

### Goals
- Provide a comprehensive, searchable database of historical F1 data (seasons, races, results, standings)
- Enable users to ask natural language questions about F1 and receive accurate, contextual answers
- Deliver a smooth, mobile-first experience for browsing race information
- Support race weekend engagement with schedules and real-time information
- Make telemetry data accessible and understandable to enthusiasts

### Non-Goals
- **Live timing and telemetry streaming** — Not building real-time lap-by-lap data feeds (rely on official F1 sources for live)
- **Social features** — No user-generated content, comments, or community forums in v1
- **Fantasy F1 or betting** — No predictive gaming or gambling features
- **Video content** — Not hosting race replays or video highlights (rights issues)
- **Multi-sport support** — F1 only; not expanding to other racing series in initial scope

---

## User Personas

### 1. **The Historian** — "Sarah"
- **Profile:** 28-year-old F1 fan, watches races casually, loves learning about F1's past
- **Goals:** Browse iconic races, compare driver stats across eras, discover fun facts
- **Pain Points:** Wikipedia is too dense, other apps focus only on current season
- **Key Features:** Historical race browsing, AI Q&A for trivia, driver/team comparisons

### 2. **The Analyst** — "Marco"
- **Profile:** 35-year-old data enthusiast, watches every race, analyzes performance trends
- **Goals:** Deep-dive into telemetry, understand race strategies, track performance over time
- **Pain Points:** Raw telemetry data is inaccessible or requires desktop tools
- **Key Features:** Telemetry data exploration, detailed race results, AI-assisted analysis

### 3. **The Weekend Warrior** — "James"
- **Profile:** 42-year-old casual fan, watches most races, follows 2-3 favorite drivers
- **Goals:** Check race weekend schedules, catch up on qualifying results, see standings
- **Pain Points:** Official F1 app is bloated, just wants quick info without clutter
- **Key Features:** Race weekend schedule, quick standings view, session results

---

## Core Features

### P0 — Must-Have (MVP)

#### 1. Historical Race Data Browsing
- **Seasons List:** Browse F1 seasons from 1950 to present
- **Race Calendar:** View race calendar for any season, with circuit info
- **Race Results:** Detailed finishing order, lap times, fastest laps, pit stops
- **Standings:** Driver and Constructor standings for any season
- **Driver/Team Profiles:** Career stats, wins, poles, championships

#### 2. AI-Powered Q&A Agent
- **Natural Language Queries:** "Who won the 1998 Belgian GP?", "How many wins does Hamilton have at Silverstone?"
- **Contextual Understanding:** Handle follow-up questions, time-based queries
- **Data Sources:** Historical race results, driver stats, team info, telemetry summaries
- **Response Format:** Concise text answers with data citations (e.g., "Lewis Hamilton: 8 wins at Silverstone (2008, 2014-2017, 2019-2021)")

#### 3. Core Mobile Navigation
- **Home Screen:** Quick access to upcoming race, recent races, AI agent
- **Search:** Find races, drivers, teams by name or year
- **Responsive Design:** Smooth scrolling, optimized for mobile screens
- **Dark/Light Mode:** System-aware theme switching

### P1 — Should-Have (Post-MVP)

#### 4. Race Weekend Companion
- **Session Schedule:** Practice, Qualifying, Race times (user timezone)
- **Live Session Status:** "Qualifying in progress", "Race starts in 2h"
- **Session Results:** Immediate access to qualifying grid, practice times
- **Push Notifications:** Opt-in reminders for sessions

#### 5. Telemetry Data Exploration
- **Lap-by-Lap Data:** Speed traces, throttle/brake inputs, gear shifts
- **Comparative Views:** Overlay two drivers' laps
- **Key Moments:** Pre-analyzed insights (e.g., "Verstappen lost 0.3s in Sector 2")
- **Educational Context:** Explain telemetry metrics for casual fans

#### 6. Enhanced AI Agent
- **Multi-Turn Conversations:** Maintain context across queries
- **Telemetry Questions:** "Show me Hamilton's fastest lap from Monaco 2023"
- **Comparison Queries:** "Compare Senna vs Prost qualifying records"

### P2 — Nice-to-Have (Future)

#### 7. Personalization
- **Favorite Drivers/Teams:** Custom dashboard with favorite's stats
- **Bookmarked Races:** Save memorable races for quick access
- **Watch History:** Track what you've explored

#### 8. Circuit Explorer
- **3D Circuit Maps:** Interactive track layouts
- **Corner Analysis:** Historical data per corner (fastest speeds, common incidents)
- **Track Evolution:** How circuits have changed over time

#### 9. Offline Mode
- **Cached Data:** Download seasons for offline browsing
- **AI Agent Cache:** Pre-cache common queries

---

## Technical Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                     React Native App                        │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │   Browse    │  │  AI Q&A      │  │  Race Weekend    │   │
│  │   History   │  │  Interface   │  │  Companion       │   │
│  └─────────────┘  └──────────────┘  └──────────────────┘   │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTPS/REST
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              .NET 10 Minimal API Gateway                    │
│  ┌──────────────┐  ┌────────────────┐  ┌────────────────┐  │
│  │   Races      │  │   AI Agent     │  │   Telemetry    │  │
│  │   API        │  │   Service      │  │   API          │  │
│  └──────────────┘  └────────────────┘  └────────────────┘  │
└────────┬────────────────────┬──────────────────────┬────────┘
         │                    │                      │
         ▼                    ▼                      ▼
┌─────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   PostgreSQL    │  │   AI/LLM Service │  │   Cache Layer    │
│   (Historical   │  │   (Azure OpenAI, │  │   (Redis)        │
│   F1 Data)      │  │   or similar)    │  │                  │
└─────────────────┘  └──────────────────┘  └──────────────────┘
```

### Technology Stack
- **Frontend:** React Native with React Native Paper (Material Design)
- **Backend:** .NET 10 Minimal API (C#)
- **Orchestration:** .NET Aspire (local dev environment, service discovery)
- **Database:** PostgreSQL (historical data, telemetry)
- **Cache:** Redis (frequently accessed data, AI query results)
- **AI/LLM:** Azure OpenAI or similar (GPT-4 for Q&A)
- **Deployment:** Docker containers, Azure Container Apps (or similar)

### Key Design Decisions
1. **Minimal API over MVC:** Lightweight, performance-focused endpoints for mobile
2. **React Native Paper:** Well-maintained, Material Design 3 compliant, cross-platform
3. **Aspire for local dev:** Unified orchestration for .NET services, React Native Metro bundler
4. **PostgreSQL over NoSQL:** Relational data (seasons → races → results) benefits from RDBMS, strong querying for AI agent

---

## Authentication & Authorization

### Auth Provider
**.NET Identity** — integrated into the .NET 10 Minimal API backend. Provides user registration, login, session management, and role-based access control.

### Login Methods
- **Email/Password:** Standard registration and login with name, email, and password
- **Passkeys:** WebAuthn/FIDO2 passkey support for passwordless authentication

### Access Tiers

| Access Level | Features | Auth Required? |
|---|---|---|
| **Anonymous** | Race browsing, historical results, standings, driver/team profiles, circuit info, cached data | No |
| **Authenticated** | Telemetry data exploration, AI agent Q&A, conversation history, personalization (P2) | Yes |

### Anonymous Access (No Login Required)
- Browse seasons, races, results, standings, driver/team profiles, circuits
- Access cached and historical data
- Full use of core mobile navigation, search, dark/light mode
- Race weekend schedule viewing (P1)

### Authenticated Access (Login Required)
- **AI Agent:** All `/ai/*` endpoints require authentication (needed for rate limiting, conversation persistence, cost tracking)
- **Telemetry:** All `/telemetry/*` endpoints require authentication
- **Conversation History:** Stored per-user, requires login to persist and retrieve
- **Personalization (P2):** Favorites, bookmarks, watch history tied to user account

### Implications for Other Features
- **API Endpoints:** Public data endpoints (seasons, races, results, drivers, constructors, circuits) serve anonymous requests. AI and telemetry endpoints return `401 Unauthorized` without a valid session.
- **AI Agent:** Login prompt must be shown before first AI query if user is unauthenticated. Rate limiting is per-user (not per-device).
- **Monetization (Future):** User accounts are now available for feature gating if freemium model is adopted.
- **Privacy:** .NET Identity stores PII (email, name). Privacy policy must cover data retention, deletion rights (GDPR/CCPA). See Q35.
- **Mobile UX:** App must gracefully handle the anonymous → authenticated transition (e.g., user browses anonymously, taps "Ask AI", gets login prompt, returns to AI after auth).

---

## Data Model

### Core Entities

#### Season
- `id` (int, PK)
- `year` (int, unique)
- `wikipedia_url` (string, nullable)

#### Race
- `id` (int, PK)
- `season_id` (int, FK → Season)
- `round` (int)
- `name` (string) — e.g., "Monaco Grand Prix"
- `circuit_id` (int, FK → Circuit)
- `date` (date)
- `wikipedia_url` (string, nullable)

#### Circuit
- `id` (int, PK)
- `name` (string) — e.g., "Circuit de Monaco"
- `location` (string) — e.g., "Monte Carlo, Monaco"
- `country` (string)
- `latitude`, `longitude` (decimal, nullable)

#### Driver
- `id` (int, PK)
- `driver_ref` (string, unique) — e.g., "hamilton"
- `number` (int, nullable)
- `code` (string, nullable) — e.g., "HAM"
- `forename`, `surname` (string)
- `date_of_birth` (date)
- `nationality` (string)
- `wikipedia_url` (string, nullable)

#### Constructor (Team)
- `id` (int, PK)
- `constructor_ref` (string, unique) — e.g., "mercedes"
- `name` (string)
- `nationality` (string)
- `wikipedia_url` (string, nullable)

#### Result
- `id` (int, PK)
- `race_id` (int, FK → Race)
- `driver_id` (int, FK → Driver)
- `constructor_id` (int, FK → Constructor)
- `grid` (int) — starting position
- `position` (int, nullable) — finishing position (null if DNF)
- `points` (decimal)
- `laps` (int)
- `time` (string, nullable) — race time or gap
- `status_id` (int, FK → Status) — Finished, Retired, etc.
- `fastest_lap` (int, nullable) — lap number of fastest lap
- `rank` (int, nullable) — rank of fastest lap

#### Status
- `id` (int, PK)
- `status` (string) — "Finished", "Accident", "Engine", etc.

#### LapTime
- `race_id` (int, FK → Race, PK composite)
- `driver_id` (int, FK → Driver, PK composite)
- `lap` (int, PK composite)
- `position` (int)
- `time` (string) — lap time (e.g., "1:32.123")
- `milliseconds` (int)

#### TelemetryData (P1 feature)
- `id` (bigint, PK)
- `race_id` (int, FK → Race)
- `driver_id` (int, FK → Driver)
- `lap` (int)
- `distance_meters` (decimal) — distance into lap
- `speed` (decimal) — km/h
- `throttle` (int) — 0-100%
- `brake` (boolean)
- `gear` (int)
- `drs` (boolean)

#### DriverStanding
- `id` (int, PK)
- `race_id` (int, FK → Race)
- `driver_id` (int, FK → Driver)
- `points` (decimal)
- `position` (int)
- `wins` (int)

#### ConstructorStanding
- `id` (int, PK)
- `race_id` (int, FK → Race)
- `constructor_id` (int, FK → Constructor)
- `points` (decimal)
- `position` (int)
- `wins` (int)

### Data Sources
- **Ergast API** (historical data up to ~2024)
- **OpenF1 API** (recent telemetry, live timing)
- **Manual curation** for missing data or corrections

---

## API Surface

### Base URL
`https://api.werace.app/v1`

### Endpoint Groups

> **Auth Legend:** 🔓 = Public (anonymous access) | 🔐 = Authenticated required

#### Seasons 🔓
- `GET /seasons` — List all seasons (with optional filters: `?from=1990&to=2000`)
- `GET /seasons/{year}` — Season details with race calendar
- `GET /seasons/{year}/standings` — Final standings (drivers + constructors)

#### Races 🔓
- `GET /races` — List races (filterable: `?season=2023`, `?circuit_id=1`)
- `GET /races/{race_id}` — Race details (circuit, date, session times)
- `GET /races/{race_id}/results` — Finishing order with lap times
- `GET /races/{race_id}/qualifying` — Qualifying results
- `GET /races/{race_id}/standings` — Standings after this race
- `GET /races/{race_id}/laps` — Lap-by-lap times (optional `?driver_id=X`)

#### Drivers 🔓
- `GET /drivers` — List all drivers (paginated)
- `GET /drivers/{driver_id}` — Driver profile
- `GET /drivers/{driver_id}/career` — Career stats summary (wins, poles, championships)
- `GET /drivers/{driver_id}/results` — Race results history

#### Constructors 🔓
- `GET /constructors` — List all teams
- `GET /constructors/{constructor_id}` — Team profile
- `GET /constructors/{constructor_id}/career` — Career stats
- `GET /constructors/{constructor_id}/results` — Race results history

#### Circuits 🔓
- `GET /circuits` — List all circuits
- `GET /circuits/{circuit_id}` — Circuit details (location, map)
- `GET /circuits/{circuit_id}/races` — All races held at this circuit

#### Telemetry (P1) 🔐
- `GET /telemetry/race/{race_id}/driver/{driver_id}/lap/{lap}` — Full telemetry for one lap
- `GET /telemetry/race/{race_id}/driver/{driver_id}/fastest-lap` — Telemetry for fastest lap
- `GET /telemetry/compare` — Compare two drivers' laps (query params: `race_id`, `driver1_id`, `driver2_id`, `lap1`, `lap2`)

#### AI Agent 🔐
- `POST /ai/query` — Submit natural language question *(requires authenticated user for rate limiting and conversation persistence)*
  - **Request:** `{ "query": "Who won the 2020 Turkish GP?", "conversation_id": "uuid" }` *(Bearer token required)*
  - **Response:** `{ "answer": "Lewis Hamilton won...", "sources": [...]", "conversation_id": "uuid" }`
- `GET /ai/conversations/{conversation_id}` — Retrieve conversation history *(user can only access own conversations)*

#### Race Weekend (P1) 🔓
- `GET /live/current` — Current or next race weekend details
- `GET /live/schedule` — Upcoming race weekend schedule (P/Q/R times)

---

## AI Agent Specification

> **⚠️ Login Required:** The AI agent is only available to authenticated users. Unauthenticated users who tap "Ask AI" must be prompted to log in first. This enables per-user rate limiting, conversation persistence, and cost tracking.

### Capabilities
The AI agent answers natural language questions about F1 using a retrieval-augmented generation (RAG) approach:

1. **Query Understanding:** Parse user question, identify intent (e.g., race result, driver stat, comparison)
2. **Data Retrieval:** Query PostgreSQL for relevant data (races, results, standings, telemetry)
3. **Context Building:** Format retrieved data into structured context for LLM
4. **Answer Generation:** LLM generates natural language response with citations
5. **Conversation Memory:** Maintain context across follow-up questions

### Example Questions
- **Simple lookup:** "Who won the 2023 Monaco Grand Prix?"
- **Statistics:** "How many pole positions does Max Verstappen have?"
- **Comparisons:** "Compare Senna and Prost's win rates"
- **Time-based:** "Who was leading the championship after Silverstone 2021?"
- **Telemetry (P1):** "Show me Hamilton's speed through Copse corner at Silverstone 2023"
- **Analytical:** "What was Ferrari's best season in terms of points?"

### Data Sources
- **Primary:** PostgreSQL historical data (seasons, races, results, standings)
- **Secondary (P1):** Telemetry database (lap traces, sector times)
- **Metadata:** Driver/team profiles, circuit information

### Technical Approach
1. **Vector Search (Future):** Embed race descriptions, driver bios for semantic search
2. **SQL Generation (MVP):** Use LLM to generate SQL queries from natural language
3. **Hybrid:** Combine keyword search, structured queries, and LLM reasoning

### Response Format
```json
{
  "answer": "Lewis Hamilton won the 2023 Monaco Grand Prix...",
  "confidence": 0.95,
  "sources": [
    {
      "type": "race_result",
      "race_id": 1234,
      "race_name": "2023 Monaco Grand Prix",
      "date": "2023-05-28"
    }
  ],
  "follow_up_suggestions": [
    "What was Hamilton's qualifying position?",
    "Who finished second?"
  ]
}
```

### Error Handling
- **No data found:** "I don't have data for that race/driver/season."
- **Ambiguous query:** "Did you mean [Driver A] or [Driver B]?"
- **Out of scope:** "I can only answer questions about Formula 1 data."

---

## Design Guidelines

### UI Framework
**React Native Paper** (Material Design 3)
- **Why:** Most popular React Native design library, comprehensive component set, active maintenance, excellent theming support
- **Alternative considered:** React Native Elements (less Material-focused)

### Design Principles
1. **Mobile-First:** Thumb-friendly tap targets, one-handed navigation where possible
2. **Content Density:** Balance information richness with readability (not too cluttered)
3. **Speed:** Instant feedback, optimistic UI updates, skeleton screens for loading
4. **Accessibility:** High contrast ratios, screen reader support, scalable fonts

### Theme
- **Light Mode:** Clean whites, subtle grays, accent colors for teams (Ferrari red, Mercedes silver, etc.)
- **Dark Mode:** True black backgrounds (OLED-friendly), muted accent colors
- **System Aware:** Automatically switch based on device settings

### Key Screens
1. **Home:** Upcoming race card, "Ask AI" search bar *(prompts login if unauthenticated)*, recent races list
2. **Season Browser:** Grid or list of seasons, filterable by decade
3. **Race Details:** Hero image (circuit), session times, results tabs (Race/Qualifying/Practice)
4. **Driver Profile:** Photo, career stats cards, results timeline
5. **AI Chat:** Chat interface with message bubbles, quick action suggestions

### Typography
- **Headers:** Bold, clear hierarchy (H1: 28pt, H2: 22pt, H3: 18pt)
- **Body:** 16pt for readability, 14pt for dense tables
- **Monospace:** Lap times, telemetry data

### Colors (Suggested Palette)
- **Primary:** #E10600 (F1 Red)
- **Secondary:** #15151E (Dark Gray/Black)
- **Accent:** #00D2BE (Teal, for highlights)
- **Success:** #00C853 (Green, for wins/poles)
- **Warning:** #FFD600 (Yellow, for flags/cautions)

---

## Success Metrics

### User Engagement
- **DAU/MAU:** Daily/Monthly Active Users ratio
- **Session Length:** Average time per session (target: 8+ minutes)
- **Screens per Session:** Number of screens viewed (target: 6+)
- **Return Rate:** % of users returning within 7 days (target: 40%)

### Feature Adoption
- **AI Agent Usage:** % of users who ask at least one question (target: 60%)
- **Historical Browse:** % of users exploring races beyond current season (target: 70%)
- **Race Weekend Companion:** % of active users during race weekends (target: 80%)

### Performance
- **App Launch Time:** < 2 seconds to interactive
- **API Response Time:** P95 < 300ms for data queries, < 2s for AI responses
- **Crash Rate:** < 0.5% of sessions

### Data Quality
- **AI Accuracy:** % of AI responses rated as correct (manual spot checks, target: 95%)
- **Data Completeness:** % of races with full results (target: 99% for seasons 2000+)

---

## Open Questions

### 1. Data Licensing & Sources
- **Q:** Do we have rights to use Ergast/OpenF1 APIs in a commercial app?
- **A:** Need legal review; may require direct licensing from FOM (Formula One Management)
- **Decision Needed By:** Before public beta

### 2. AI Backend Provider
- **Q:** Azure OpenAI vs. self-hosted model (e.g., Llama)?
- **Options:**
  - Azure OpenAI: Faster to market, higher quality, ongoing costs
  - Self-hosted: More control, lower variable costs, requires ML expertise
- **Decision Needed By:** Sprint 2 (architecture finalization)

### 3. Telemetry Data Access
- **Q:** Where do we get lap-by-lap telemetry data?
- **Options:**
  - OpenF1 API (recent seasons, limited history)
  - FastF1 Python library (good coverage, needs ETL pipeline)
  - Manual data partnerships (expensive)
- **Decision Needed By:** Before P1 features (post-MVP)

### 4. Push Notifications
- **Q:** What notification service? (Firebase, Azure Notification Hubs, OneSignal)
- **Consideration:** Cross-platform support, cost at scale
- **Decision Needed By:** P1 feature planning

### 5. Offline Mode Scope
- **Q:** How much data to allow for offline download?
- **Options:**
  - Single season (~50MB)
  - Selected races only
  - AI agent cache only (no full data)
- **Decision Needed By:** P2 planning

### 6. Monetization
- **Q:** Free vs. freemium vs. paid?
- **Options:**
  - Free with ads (requires ad platform integration)
  - Freemium (basic free, premium features like telemetry for $2.99/month)
  - Paid upfront ($4.99 one-time)
- **Decision Needed By:** Before app store submission

### 7. Real-Time Data
- **Q:** Do we integrate live timing during races?
- **Consideration:** Requires F1 official data feed (expensive), or scraping (legal risk)
- **Decision Needed By:** P1 scoping

---

## Appendix: Related Documents

- **Technical Design Doc:** (TBD — detailed API specs, database schema DDL)
- **AI Agent Design Doc:** (TBD — RAG architecture, prompt engineering, evaluation)
- **Mobile Design Mockups:** (TBD — Figma files)
- **Data Pipeline Spec:** (TBD — ETL from Ergast/OpenF1 to PostgreSQL)

---

**Document Status:** Draft for review  
**Next Steps:**
1. Team review (Richard, Dinesh, Gilfoyle, Jared)
2. Stakeholder approval (Vincent)
3. Break down into epics/stories for sprint planning
