# Decision: Domain and Infrastructure project structure

**By:** Gilfoyle (Backend Developer)
**Date:** 2026-03-04
**Context:** E2 Data Pipeline — foundational data layer projects

## Decisions

### 1. Status entity property named `StatusText`

The `status` table has a `status` column. Naming the C# property `Status` would conflict with the entity class name. Property is named `StatusText` with an explicit `HasColumnName("status")` in the EF Core configuration.

### 2. Snake case via EFCore.NamingConventions

All column and table naming uses `UseSnakeCaseNamingConvention()` from the `EFCore.NamingConventions` package, applied at DbContext registration in `Program.cs`. Entity configurations also set explicit table names for clarity. This means PascalCase C# properties automatically map to snake_case columns without manual `HasColumnName` calls (except the `StatusText` override).

### 3. No IdentityDbContext yet

`WeRaceDbContext` inherits from `DbContext`, not `IdentityDbContext`. Identity integration is E4 Auth scope. When that lands, the base class changes and Identity schema separation gets added to `OnModelCreating`.

### 4. Aspire database resource reference

`AppHost.cs` now correctly separates the postgres server resource from the database resource. `WithReference(db)` passes the connection string for the `werace` database specifically, not the server.
