# Data Model Recommendations — Q3, Q4, Q5

**Author:** Gilfoyle (Backend Developer)
**Date:** 2026-02-26
**Status:** Proposals for team decision

---

## Q3 — Sprint Races (2021+)

**Recommendation: Separate `sprint_results` table**

**Decision:** Model sprint races as a separate `sprint_results` table mirroring the `results` table structure.

**Rationale:**

1. **Jolpica alignment.** The Ergast/Jolpica database dump stores sprint results in a separate `sprintResults` table. Matching this means near-zero transformation during import. Fighting the source data format burns time for no benefit.

2. **Clean API surface.** `GET /races/{id}/results` returns race results. `GET /races/{id}/sprint` returns sprint results. No `session_type` filter needed. Clients ask for what they want and get it.

3. **Sprint format keeps changing.** In 2021 it was "Sprint Qualifying" → 2022 became "Sprint" → 2023 added "Sprint Shootout" → 2024 changed qualifying format again. A separate table isolates this volatility. If FOM adds yet another format in 2027, we add a table, not a migration that touches 1000+ rows of race results.

4. **No data model pollution.** Adding `session_type` to `results` would mean every query against race results needs a `WHERE session_type = 'RACE'` clause. That's a bug waiting to happen.

**Sprint race indicators on `races` table:** The `races` table includes `sprint_date` and `sprint_time` columns. If `sprint_date IS NOT NULL`, the race weekend includes a sprint. This also comes from the Jolpica dump directly.

**Sprint Shootout / Sprint Qualifying:** Not modeled separately. The qualifying table already captures grid positions. Sprint shootout results that determine sprint grid can be inferred from `sprint_results.grid`. If we later need explicit sprint qualifying times, we add a `sprint_qualifying` table — doesn't affect existing schema.

---

## Q4 — Qualifying: Separate Entity or Flag in Result?

**Recommendation: Separate `qualifying` table**

**Decision:** Qualifying is a separate `qualifying` entity, not a flag in the `results` table.

**Rationale:**

1. **Different data shape.** Qualifying has Q1/Q2/Q3 times per driver. Race results have finishing position, laps completed, race time, fastest lap, pit stops. These are structurally different data. Cramming them into one table means half the columns are NULL half the time.

2. **Jolpica alignment.** Ergast/Jolpica stores qualifying in a separate `qualifying` table with `q1`, `q2`, `q3` columns. Direct import, no transformation.

3. **Historical format variation.** Qualifying formats have changed many times: single-lap (pre-2003), aggregate (2003-2005), knockout (2006+), one-shot (briefly in 2005). The separate table with nullable Q1/Q2/Q3 handles all formats — older data just has `q1` populated, modern data has all three.

4. **API clarity.** `GET /races/{id}/qualifying` maps directly to a table query. No filtering, no confusion.

**Schema:**

```sql
CREATE TABLE qualifying (
    id              SERIAL PRIMARY KEY,
    race_id         INT NOT NULL REFERENCES races(id),
    driver_id       INT NOT NULL REFERENCES drivers(id),
    constructor_id  INT NOT NULL REFERENCES constructors(id),
    number          INT NOT NULL,
    position        INT NOT NULL,
    q1              VARCHAR(255),  -- NULL for pre-knockout era
    q2              VARCHAR(255),  -- NULL if eliminated in Q1
    q3              VARCHAR(255)   -- NULL if eliminated in Q2
);
```

---

## Q5 — Pit Stops: In Scope for MVP?

**Recommendation: Yes — include pit stops in Phase 1**

**Decision:** Include `pit_stops` table and `GET /races/{id}/pit-stops` endpoint in Phase 1.

**Rationale:**

1. **Zero additional development cost.** The Jolpica dump includes a `pitStops` table. We import it alongside everything else. One more table in the schema, one more COPY command in the import pipeline, one more endpoint in the API. Maybe 2 hours of work.

2. **High browsing value.** Pit stop data is one of the most interesting aspects of race strategy. Users browsing historical races want to see "Verstappen 1-stop vs Hamilton 2-stop" — that's a P0 data browsing feature, not a P1 feature.

3. **AI foundation.** Pit stop data enables some of the best AI queries: "What was the average pit stop time for Red Bull in 2023?", "Which driver had the fastest pit stop in 2022?" These views are trivial to build on a table that already exists.

4. **Data availability.** Pit stop data is available from 2012 onwards in the Jolpica dump. Pre-2012 rows simply don't exist — no null handling needed, no sparse data problem.

**Cost-benefit:** ~2 hours of work for a feature users will notice and the AI will use. Not including it would mean re-importing data and adding the table later — more work, not less.

**Scope boundary:** Pit stop data in Phase 1 is **read-only browsing** — show pit stops as part of race results. No pit stop analysis tools, no strategy visualizations. Those are P1/P2 if ever.
