# Session Log — 2026-02-26: Data Source Resolution (Q12)

## What Happened

Monica (Product Owner) resolved Q12 from the PRD review based on stakeholder input from Vincent. The deprecated Ergast API is replaced by the Jolpica API, an Ergast-compatible replacement that also provides database dump files for direct PostgreSQL import.

## Changes Made

- **PRD updated:** All Ergast references replaced with Jolpica across Data Sources, Open Questions, and Appendix Data Pipeline sections.
- **PRD-REVIEW updated:** Q12 marked as ✅ RESOLVED with full resolution details.
- **decisions.md updated:** Q12 blocker marked resolved with resolution details and follow-up items.
- **Follow-ups filed:** 6 sub-questions in `.squad/decisions/inbox/monica-data-source-followups.md` covering dump format, sync strategy, licensing, data freshness, completeness, and 2025+ coverage.

## Remaining Critical Blockers

- Q18 — AI Safety Rails (unresolved)
- Q1 — MVP Scope Assessment (unresolved)

## Agents Affected

- **Gilfoyle (Backend):** Data pipeline design can now target Jolpica dump import instead of Ergast ETL.
- **Richard (Lead):** Q12 is resolved; 2 of 4 critical blockers now cleared.
