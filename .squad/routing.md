# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & scope | Richard | System design, API contracts, data models, tech decisions |
| React Native UI | Dinesh | Screens, components, navigation, design system, animations |
| .NET API & backend | Gilfoyle | Minimal API endpoints, database, Aspire setup, AI agent integration |
| Testing & quality | Jared | Unit tests, integration tests, edge cases, test infrastructure |
| Requirements & functional design | Monica | User stories, acceptance criteria, stakeholder Q&A, feature specs |
| Code review | Richard | Review PRs, check quality, suggest improvements |
| F1 data & domain | Gilfoyle + Richard | Historical data ingestion, telemetry, race models |
| AI agent features | Gilfoyle | LLM integration, prompt engineering, response formatting |
| Mobile UX & design | Dinesh | User flows, responsive layout, accessibility |
| DevOps & infrastructure | Gilfoyle | Aspire configuration, deployment, CI/CD |
| Scope & priorities | Richard | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Richard |
| `squad:richard` | Architecture, scope, review tasks | Richard |
| `squad:dinesh` | Frontend, UI, React Native tasks | Dinesh |
| `squad:gilfoyle` | Backend, API, database, AI tasks | Gilfoyle |
| `squad:jared` | Testing, QA, quality tasks | Jared |
| `squad:monica` | Requirements, functional design, stakeholder tasks | Monica |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
