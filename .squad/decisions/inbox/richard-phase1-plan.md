### 2026-02-26: Phase 1 Plan — Architectural Decisions

**By:** Richard (Lead / Architect)
**Status:** 🟢 Ready for execution
**Document:** `docs/PHASE1-PLAN.md`

---

**Context:** All 4 critical blockers resolved. Phase 1 sprint planning complete. This captures the key architectural decisions embedded in the plan.

**Decisions:**

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **5 sprints × 1 week** (not 4 or 6) | 5 weeks balances the 4–6 week window. Gives 1 sprint of buffer over minimum without dragging. |
| 2 | **6 epics: Scaffolding, Data Pipeline, API, Auth, Mobile UI, AI Foundations** | Clean separation of concerns. Each epic has a single primary owner. Dependencies flow left-to-right. |
| 3 | **Offset-based pagination, default pageSize=20, max 100** | Simpler than cursor-based for historical data that rarely changes. Cursor adds complexity without benefit for static datasets. Revisit for Phase 2 live data. |
| 4 | **Consistent JSON envelope: `{ "data", "pagination", "error" }`** | Every endpoint returns the same shape. Frontend never guesses. Errors are structured, not strings. |
| 5 | **Client-side search for Phase 1, evaluate backend search in Phase 2** | Driver/constructor/circuit lists are small enough (< 1000 items) for client-side filtering. Backend search only justified if AI queries need it or datasets grow. |
| 6 | **Mock data decoupling: Dinesh builds UI against API contracts, not live endpoints** | Prevents Gilfoyle bottleneck from blocking frontend. API contracts published in S1, screens built in S2 with JSON fixtures, connected in S3. |
| 7 | **Passkeys as best-effort: ship email/password, add passkeys if feasible** | FIDO2 in React Native is uncertain. Email/password satisfies all Phase 1 requirements. Backend supports passkeys regardless. |
| 8 | **5 database views as AI foundations** | `v_driver_career_stats`, `v_constructor_season_stats`, `v_race_summary`, `v_head_to_head`, `v_circuit_records`. Zero cost now, immediate value for Phase 2 LLM context. |
| 9 | **Redis for caching AND per-user request tracking** | Single Redis instance serves two purposes. Historical data cached 24h, current season 1h. Per-user counters ready for Phase 2 rate limiting. |
| 10 | **Richard as fallback for API endpoints if Gilfoyle bottleneck hits** | Lead picks up simpler endpoints (circuits, status) to keep critical path on schedule. |

**Risks Accepted:**

- Jolpica dump format unknown — Gilfoyle investigates Day 1 before committing to pipeline approach
- Passkey React Native maturity — fallback to email/password only is acceptable for Phase 1
- Single backend engineer (Gilfoyle) on critical path — mitigated by mock data decoupling and Richard as backup

**Action:** Team executes from `docs/PHASE1-PLAN.md` starting Sprint 1.
