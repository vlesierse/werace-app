# Project Context

- **Owner:** Vincent Lesierse
- **Project:** WeRace — Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A
- **Stack:** React Native (frontend), .NET 10 Minimal API (backend), Aspire (dev orchestration), AI agent (F1 Q&A)
- **Created:** 2026-02-23

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-23 — PRD Review

- **PRD location:** `docs/PRD.md` (v1.0, Draft)
- **PRD review output:** `docs/PRD-REVIEW.md` — 36 stakeholder questions, 8 vague requirements flagged, 10 recommendations
- **Critical gaps found:** No authentication model, Ergast API is deprecated (primary data source), AI agent lacks security guardrails (SQL injection risk from LLM-generated queries), no privacy policy
- **MVP scope concern:** Three major features (data browsing + AI agent + core nav) may be too ambitious for a first release
- **Missing data model entities:** Qualifying results, Sprint races, PitStop data — all referenced in features but absent from schema
- **Owner preference:** Vincent named the product "WeRace" — trademark clearance flagged as open question
- **Team decisions to respect:** PostgreSQL (relational fit), React Native Paper (MD3), REST over GraphQL, RAG with SQL generation for AI agent

### 2026-02-23 — Q25 Authentication Resolved

- **Question:** Q25 (Authentication Model) — the #1 priority blocker from PRD review
- **Vincent's answer:** .NET Identity, email/password + passkeys, anonymous browsing allowed, auth required for AI agent and telemetry
- **PRD updated:** Added "Authentication & Authorization" section between Technical Architecture and Data Model
- **API Surface updated:** All endpoint groups now marked with 🔓 (public) or 🔐 (authenticated). AI and Telemetry endpoints require auth.
- **AI Agent section updated:** Added login-required notice at top of specification
- **Follow-up questions filed:** 7 sub-questions in `.squad/decisions/inbox/monica-auth-followups.md` — most critical are Apple Sign-In requirement (F1), anonymous→auth UX flow (F3), and passkey feasibility in React Native (F4)
- **Remaining critical blockers:** Q12 (data source), Q18 (AI safety rails), Q1 (MVP scope) still unresolved

### 2026-02-26 — Q12 Data Source Resolved

- **Question:** Q12 (Data Source) — Ergast API deprecated, what replaces it?
- **Vincent's answer:** Jolpica is an Ergast-compatible API built on the Ergast project. Provides both API access and database dump files for direct import.
- **Key facts about Jolpica:** Drop-in Ergast replacement, compatible API interface, exposes database as dump files for PostgreSQL import
- **PRD updated:** Data Sources section now references Jolpica API + dump files instead of Ergast. Open Questions Q1 updated for Jolpica licensing. Appendix Data Pipeline Spec updated to reference dump import instead of Ergast ETL.
- **PRD-REVIEW updated:** Q12 marked as ✅ RESOLVED. Recommendation #2 marked as ✅ RESOLVED with strikethrough.
- **decisions.md updated:** Q12 blocker marked as ✅ RESOLVED with full resolution details and follow-up items.
- **Follow-up questions filed:** 6 sub-questions in `.squad/decisions/inbox/monica-data-source-followups.md` — most critical are dump format compatibility (F2), sync strategy (F5), and licensing terms (F4)
- **Remaining critical blockers:** Q18 (AI safety rails), Q1 (MVP scope) still unresolved
