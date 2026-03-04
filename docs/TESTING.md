# Testing Strategy — WeRace

**Author:** Jared (Tester)
**Date:** 2026-02-26
**Status:** Sprint 1 scaffolding — infrastructure in place, real tests start Sprint 2

---

## Test Pyramid

WeRace follows a standard test pyramid. Integration tests are preferred over heavy mocking — test real behavior whenever practical.

```
        ╱  E2E  ╲          ← 3 core flows (Sprint 4)
       ╱─────────╲
      ╱ Integration╲       ← API + data pipeline (Sprint 2-3)
     ╱───────────────╲
    ╱    Unit Tests    ╲    ← Endpoints, domain logic (Sprint 2+)
   ╱─────────────────────╲
```

| Layer | What | Tools | When |
|-------|------|-------|------|
| **Unit** | Individual endpoint handlers, domain logic, data mapping | xUnit, FluentAssertions | Every PR |
| **Integration** | API endpoints via HTTP, data pipeline import → query, auth flows | WebApplicationFactory, TestContainers | Every PR |
| **E2E** | Full user flows through the mobile app | Detox or Maestro (TBD) | Nightly / pre-release |
| **Performance** | P95 latency for top 10 endpoints | k6 or custom benchmarks | Sprint 5 baseline |

## Running Tests

### Backend (.NET)

```bash
# Run all backend tests
dotnet test tests/WeRace.Api.Tests/

# Run with coverage report
dotnet test tests/WeRace.Api.Tests/ --collect:"XPlat Code Coverage"

# Run a specific test class
dotnet test tests/WeRace.Api.Tests/ --filter "FullyQualifiedName~HealthCheckTests"
```

### Frontend (React Native / Expo)

```bash
# Run all frontend tests
cd src/app && npm test

# Run with coverage
cd src/app && npm test -- --coverage

# Run a specific test file
cd src/app && npm test -- --testPathPattern="App.test"
```

## Coverage Targets

| Area | Target | Rationale |
|------|--------|-----------|
| API endpoints | ≥ 80% | Every endpoint must have happy path + error cases |
| Domain logic | ≥ 90% | Pure logic — no excuse for gaps |
| Infrastructure | ≥ 70% | Data access tested via integration, not unit mocks |
| Frontend components | ≥ 80% | Render tests + interaction tests |
| **Overall floor** | **≥ 80%** | Quality gate — enforced in CI |

Coverage is collected by `coverlet` (backend) and Jest's built-in coverage (frontend).

## Test Naming Conventions

### Backend (C#)

```
MethodName_StateUnderTest_ExpectedBehavior
```

Examples:

```csharp
GetSeasons_WhenDataExists_ReturnsOkWithSeasonList()
GetRace_WhenRaceNotFound_ReturnsNotFound()
GetDriverStandings_WithPagination_ReturnsCorrectPage()
HealthCheck_ReturnsOk()
```

### Frontend (TypeScript)

```
it('should [expected behavior] when [state/action]')
```

Examples:

```typescript
it('should render season list when data loads')
it('should show error message when API call fails')
it('should navigate to race details when season row is tapped')
```

## Test Data Strategy

- **Source of truth:** Jolpica F1 historical data (seasons 1950–present)
- **Fixtures:** Use real F1 data as test fixtures — not random/generated data
- **Key fixture seasons:** 2023 (recent, complete), 1950 (first season, edge cases), 2024 (latest)
- **Integration tests:** Use TestContainers for PostgreSQL with seeded data
- **Frontend:** Mock API responses using MSW (Mock Service Worker) with realistic payloads

## Test Categories

Tests are organized by what they verify:

| Category | Tag | Sprint |
|----------|-----|--------|
| Health checks | `health` | S1 |
| Season endpoints | `seasons` | S2 |
| Race endpoints | `races` | S2 |
| Driver endpoints | `drivers` | S3 |
| Constructor endpoints | `constructors` | S3 |
| Circuit endpoints | `circuits` | S3 |
| Standing endpoints | `standings` | S3 |
| Data import pipeline | `import` | S3 |
| Auth flows | `auth` | S4 |
| E2E flows | `e2e` | S4 |
| Performance baselines | `perf` | S5 |

## Project Structure

```
tests/
├── WeRace.Api.Tests/               # API endpoint integration tests
│   ├── WeRace.Api.Tests.csproj
│   ├── GlobalUsings.cs
│   ├── HealthCheckTests.cs          # GET /health → 200
│   └── (Sprint 2+: endpoint test files)
├── WeRace.Infrastructure.Tests/     # Data access tests (Sprint 3)
└── WeRace.DataImport.Tests/         # Import pipeline tests (Sprint 3)

src/app/
└── __tests__/
    ├── App.test.tsx                  # Basic render test
    └── (Sprint 2+: component test files)
```

## Quality Gates

Before any PR merges to `main`:

1. All tests pass (zero failures)
2. Coverage meets floor thresholds
3. No critical/high-severity bugs open (Phase 1 ship requirement)
4. New endpoints have corresponding test coverage

## CI Integration (TODO — Sprint 2)

- Run `dotnet test` on every PR
- Run `npm test` on every PR
- Publish coverage reports as PR comments
- Block merge if coverage drops below floor
