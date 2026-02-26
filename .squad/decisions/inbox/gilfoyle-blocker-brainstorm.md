# Gilfoyle — Blocker Brainstorm: Q18 & Q1

**Date:** 2026-02-26
**For:** Monica (Product Owner) — synthesize into proposals for Vincent

---

## Blocker 1: Q18 — AI Safety Rails

### 1. How I'd implement safe SQL generation in .NET 10

Use `Microsoft.Extensions.AI` (the new abstraction in .NET 10) with Azure OpenAI as the backing provider. The LLM call lives in an `AiQueryService` that:

1. Takes user question + schema context → sends to LLM with a system prompt constraining it to output only `SELECT` statements.
2. Receives raw SQL string from LLM.
3. Passes it through a validation pipeline (see #3 below) before it touches the database.
4. Executes validated SQL via a **read-only** `DbContext` / `NpgsqlConnection` pointed at a read-only PostgreSQL user.
5. Passes results back through the LLM for natural language formatting.

Libraries:
- `Microsoft.Extensions.AI` — LLM abstraction
- `Azure.AI.OpenAI` — provider
- `Npgsql` / EF Core — database access
- No ORM for the AI-generated queries; use raw `NpgsqlCommand` with a dedicated read-only connection string
- For SQL parsing: use a lightweight SQL parser (see #3)

No magic frameworks. Boring pipeline. Each step is independently testable.

### 2. Read-only enforcement

**Both. Belt and suspenders.**

**Layer 1 — Database user permissions:**
- Create a dedicated PostgreSQL role `werace_ai_readonly` with `GRANT SELECT` on specific tables/views only. No `INSERT`, `UPDATE`, `DELETE`, `CREATE`, `DROP`, `TRUNCATE`. No access to `pg_catalog` or system tables. No access to user/auth tables.
- The AI query connection string uses this role exclusively. Separate from the main app connection string.

**Layer 2 — Query parsing/allowlist:**
- Before execution, parse the SQL and reject anything that isn't a `SELECT`. Reject `INTO`, `CALL`, function calls, CTEs that write, `COPY`, etc.
- This is defense-in-depth. Even if the parser has a bug, the DB role prevents damage.

**Layer 3 — Statement timeout:**
- Set `statement_timeout` on the read-only role (e.g., 5 seconds). Prevents runaway queries from locking resources.

The DB role is the real safety net. Everything else is defense-in-depth.

### 3. Query validation pipeline

```
User question
  → LLM generates SQL
  → Step 1: Basic text checks (reject if contains INSERT/UPDATE/DELETE/DROP/TRUNCATE/ALTER/CREATE/GRANT — simple regex, catches obvious garbage fast)
  → Step 2: SQL parse (use a C# SQL parser like `Microsoft.SqlServer.TransactSql.ScriptDom` adapted for PostgreSQL syntax, or a simpler regex-based tokenizer. Alternatively, call `EXPLAIN` on the query without executing it — PostgreSQL will parse it and reject syntax errors without running it.)
  → Step 3: Table allowlist check (extracted table names must be in {seasons, races, results, drivers, constructors, circuits, lap_times, driver_standings, constructor_standings, statuses} — no user tables, no auth tables)
  → Step 4: Operation allowlist (only SELECT; no subqueries with DML, no function calls to pg_exec or similar)
  → Step 5: Query complexity check (reject queries with more than N joins, or nested subqueries beyond depth M — prevents runaway cartesian products)
  → Step 6: Execute via read-only connection with statement_timeout
  → Step 7: Row limit enforcement (LIMIT 1000 appended if not present; prevents accidental full-table dumps)
  → Results back to LLM for formatting
```

For the SQL parser: I'd start with `EXPLAIN (FORMAT JSON)` as the parse step. PostgreSQL parses the query plan without executing. If `EXPLAIN` fails, the SQL is invalid. If it succeeds, we get the plan and can inspect which tables are accessed. This is simpler and more reliable than writing our own parser for PostgreSQL dialect. Run `EXPLAIN` on the read-only connection so even if something slips through, no writes are possible.

### 4. Schema exposure

**Subset. Not full schema. Never auth tables.**

The LLM system prompt gets:
- Table names and column names for the F1 data tables only (the 10 tables listed in PRD data model).
- Column types and brief descriptions (e.g., `results.position — finishing position, NULL if DNF`).
- Key relationships (foreign keys) so it can write correct JOINs.
- A few example queries with expected output shapes.

**Abstracted views are overkill for MVP.** The F1 schema is simple enough (~10 tables) that the LLM can handle it directly. If we later add complexity, we can create PostgreSQL views that simplify the schema for AI consumption (e.g., a `v_race_results` view that pre-joins race + driver + constructor + status).

**What the LLM never sees:** User tables, auth tables, conversation history tables, any infrastructure tables.

### 5. Rate limiting implementation

Use the built-in `System.Threading.RateLimiting` middleware in .NET 10:

```csharp
builder.Services.AddRateLimiter(options =>
{
    // Per-user AI query rate limit
    options.AddPolicy("ai-per-user", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,           // max burst
                ReplenishmentPeriod = TimeSpan.FromHours(1),
                TokensPerPeriod = 10,      // 10 queries/hour sustained
                QueueLimit = 0             // no queueing, just reject
            }));
});
```

For daily caps (e.g., 100 queries/day per user): store a counter in PostgreSQL or Redis. Check before executing. The built-in rate limiter handles burst/throughput; the DB counter handles daily budget.

Return `429 Too Many Requests` with `Retry-After` header and a friendly message ("You've used your daily AI quota. Resets at midnight UTC.").

### 6. Cost control

**Track token usage per request, per user, per day.**

- After each LLM call, read `Usage.TotalTokens` from the Azure OpenAI response.
- Store in a `ai_usage` table: `(user_id, date, prompt_tokens, completion_tokens, total_tokens, estimated_cost_usd)`.
- Set a daily token budget per user (e.g., 50,000 tokens/day). Check before each request.
- Set a global daily budget as a circuit breaker. If total daily spend across all users exceeds threshold, disable AI endpoint and return 503 with explanation.
- Expose usage stats to the user: "You've used 12,000 of 50,000 tokens today."
- Weekly: run a report on actual Azure OpenAI costs vs. tracked tokens. Alert if divergence.

For cost estimation: GPT-4o is ~$2.50/1M input tokens, ~$10/1M output tokens (as of early 2026). A typical F1 query with schema context is ~2,000 input tokens + ~500 output tokens ≈ $0.01/query. At 100 queries/user/day, that's $1/user/day worst case. This is manageable but needs monitoring.

### 7. Fallback behavior

**Invalid SQL:**
1. Log the failed query + user question for debugging.
2. Retry once with a more constrained prompt ("The previous SQL was invalid. Generate only a simple SELECT query.").
3. If retry fails, return a graceful error: "I couldn't find an answer to that question. Try rephrasing, or browse the data directly." Include link to relevant browse endpoint if possible.

**Query timeout (>5s):**
1. PostgreSQL `statement_timeout` kills the query.
2. Return: "That question requires a complex query that timed out. Try a more specific question."
3. Log for analysis — recurring timeouts indicate missing indexes or overly complex LLM output.

**LLM service unavailable:**
1. Return 503: "AI assistant is temporarily unavailable. You can still browse all F1 data directly."
2. Implement circuit breaker pattern (`Polly` library) — after N consecutive failures, stop calling LLM for M seconds.

**Empty results:**
1. Not an error. LLM formats: "No results found for that query. This might mean the data isn't in our database, or the question is outside our coverage."

### 8. What concerns me most

**Query correctness, not security.** Security is solvable with the DB role + validation pipeline. What's harder:

1. **The LLM will generate wrong SQL that returns wrong data, and the user won't know.** A query that runs successfully but joins incorrectly or filters wrong will produce confident-sounding wrong answers. This is the hardest problem and there's no silver bullet. Mitigation: curate a test suite of known questions → expected SQL → expected results. Run it regularly. Log all generated SQL for audit.

2. **Schema changes break the AI.** Every time we modify a table, we need to update the system prompt schema context. If they drift, the LLM generates SQL against a stale schema. Mitigation: generate the schema context programmatically from the actual database, not a hardcoded string.

3. **Cost unpredictability.** One viral moment and 10,000 users hit the AI endpoint simultaneously. The per-user caps help but total spend can still spike. Need the global circuit breaker.

---

## Blocker 2: Q1 — MVP Scope Assessment

### 1. Effort estimate

**Data browsing API alone:** ~2-3 weeks for a single backend dev.
- PostgreSQL schema + EF Core entities + migrations: 3-4 days
- Jolpica dump import pipeline: 2-3 days
- REST endpoints (seasons, races, results, drivers, constructors, circuits, standings): 4-5 days
- Auth (Identity + passkeys): 2-3 days
- Aspire orchestration + Docker setup: 1-2 days

**Adding the AI agent on top:** +2-3 weeks.
- LLM integration + prompt engineering: 3-4 days
- SQL validation pipeline (parsing, allowlisting, testing): 3-4 days
- Conversation persistence + history endpoints: 2 days
- Rate limiting + cost tracking: 2 days
- Testing, edge cases, prompt tuning: 3-4 days (this is the part that always expands)

**The AI agent roughly doubles the backend effort.** And the testing surface is much larger because LLM output is non-deterministic.

### 2. Can AI be cleanly deferred?

**Yes, cleanly.** The data browsing API and the AI agent are architecturally separate concerns.

The data browsing API is standard CRUD: EF Core → PostgreSQL → JSON responses. The AI agent is a separate service that *reads from the same database* but through its own connection and its own code path.

If we build data browsing first:
- The schema is the same either way (F1 data tables don't change based on whether AI exists).
- The REST endpoints don't change (AI gets its own `/ai/*` routes).
- Adding AI later is literally: add a new service class, new endpoints, new LLM dependency. No changes to existing code.

The only thing that gets slightly harder if deferred: if we don't think about AI-friendly schema from the start, we might miss useful indexes or denormalized views. But that's a minor schema migration, not a redesign.

### 3. Foundations to include even if AI is deferred

1. **Schema generation script:** A script/utility that dumps the F1 schema (table names, columns, types, relationships) into a format suitable for an LLM system prompt. Takes 30 minutes to write. Pays off immediately when AI work starts.

2. **Read-only database role:** Create the `werace_ai_readonly` PostgreSQL role in the initial migration, even if nothing uses it yet. It's one SQL statement.

3. **Conversation history table:** Include `ai_conversations` and `ai_messages` tables in the initial schema. They don't cost anything to have, and avoid a migration later. (Optional — a migration is fine too.)

4. **Usage tracking table:** Same logic. `ai_usage (user_id, date, tokens, cost)` — cheap to include now.

5. **Auth plumbing:** This is needed regardless (telemetry is also auth-gated). So Identity + user model + `[Authorize]` middleware is not AI-specific work.

Items 1-2 are the only ones I'd insist on. The rest are nice-to-have.

### 4. Gut call

**Defer AI to post-MVP. Ship data browsing first.**

Reasoning:
- The data browsing API is the foundation. It needs to be solid, well-tested, and performant. That's enough scope for an MVP.
- AI adds risk: non-deterministic output, prompt engineering iteration, cost management, security review. Each of these can expand timelines.
- AI doesn't change the architecture. Adding it later is clean.
- Users can still get value from browsing F1 data without AI. AI without data is useless; data without AI is still useful.
- We can ship, get user feedback on what questions people *actually* want to ask, and then tune the AI prompts accordingly. Building AI first means guessing.

The only counterargument: if the pitch to Vincent/users is "AI-powered F1 app," then shipping without AI might feel incomplete. That's a product call, not a technical one. From a backend perspective, deferring AI is strictly better for delivery risk.

---

**End of brainstorm. Monica, do with this what you will.**
