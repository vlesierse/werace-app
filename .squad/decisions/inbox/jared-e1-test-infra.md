# Decision: Test Infrastructure Location & Patterns

**By:** Jared (Tester)
**Date:** 2026-02-26
**Context:** Sprint 1 — E1 Project Scaffolding

## Decisions

### 1. Test project location follows TECHNICAL-FOUNDATION.md

Tests live at `tests/WeRace.Api.Tests/` (not `src/api/WeRace.Api.Tests/`), matching the solution structure defined in `docs/TECHNICAL-FOUNDATION.md`. This keeps source and test code separated at the top level.

### 2. Placeholder tests with commented-out real implementations

Since Gilfoyle and Dinesh haven't created the API or app projects yet, all test files contain:
- A working placeholder test to prove the toolchain is wired
- Commented-out real test code (health check, app render) ready to uncomment once dependencies exist

### 3. WebApplicationFactory pattern for backend integration tests

Backend tests use `WebApplicationFactory<Program>` to spin up the API in-memory. This gives us real HTTP integration testing without external infrastructure. The project reference to `WeRace.Api` is commented out until that project exists.

### 4. xUnit + FluentAssertions + coverlet for backend

- **xUnit** — standard .NET test framework, works with `dotnet test`
- **FluentAssertions** — readable assertions (`response.StatusCode.Should().Be(HttpStatusCode.OK)`)
- **coverlet** — cross-platform code coverage collection
- **Microsoft.AspNetCore.Mvc.Testing** — in-memory test server

### 5. Jest for frontend (Expo default)

Expo comes with Jest pre-configured. Frontend tests use React Native Testing Library pattern. Tests live at `src/app/__tests__/`.

### 6. Test naming conventions

- Backend: `MethodName_StateUnderTest_ExpectedBehavior`
- Frontend: `it('should [behavior] when [condition]')`

## Impact

All team members should:
- Run `dotnet test tests/WeRace.Api.Tests/` to verify backend tests
- Run `cd src/app && npm test` to verify frontend tests (once app exists)
- Follow naming conventions in `docs/TESTING.md`
- Gilfoyle: add `tests/WeRace.Api.Tests/WeRace.Api.Tests.csproj` to `WeRace.sln` when creating the solution
