# Team Roster

> Formula 1 mobile companion app — historical race data, live info, and AI-powered Q&A

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Richard | Lead | `.squad/agents/richard/charter.md` | ✅ Active |
| Dinesh | Frontend Dev | `.squad/agents/dinesh/charter.md` | ✅ Active |
| Gilfoyle | Backend Dev | `.squad/agents/gilfoyle/charter.md` | ✅ Active |
| Jared | Tester | `.squad/agents/jared/charter.md` | ✅ Active |
| Monica | Product Owner | `.squad/agents/monica/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |
| @copilot | Coding Agent | `copilot-instructions.md` | 🤖 Autonomous |

## Project Context

- **Owner:** Vincent Lesierse
- **Stack:** React Native, .NET 10 Minimal API, Aspire, AI agent
- **Description:** Mobile app for Formula 1 fans — historical race data, live info, and AI-powered Q&A about races, results, and telemetry
- **Created:** 2026-02-23

## Coding Agent

<!-- copilot-auto-assign: true -->

### Capabilities

| Category | Fit | Notes |
|----------|-----|-------|
| Single-file bug fixes | 🟢 Good fit | Clear scope, isolated changes |
| Multi-file feature implementation | 🟢 Good fit | Can follow patterns across files |
| Test writing | 🟢 Good fit | Unit and integration tests from specs |
| Dependency updates | 🟢 Good fit | Package bumps, lockfile updates |
| API endpoint scaffolding | 🟢 Good fit | .NET minimal API, standard patterns |
| React Native UI components | 🟡 Needs review | Can scaffold, but design review needed |
| Architecture decisions | 🔴 Not suitable | Requires team discussion and Lead approval |
| Database schema changes | 🟡 Needs review | Can implement, needs Gilfoyle review |
| AI/LLM prompt engineering | 🔴 Not suitable | Requires domain expertise and iteration |
| Aspire configuration | 🟡 Needs review | Can follow patterns, needs infra review |
