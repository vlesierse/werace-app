# E1 Backend Scaffolding Decisions

**By:** Gilfoyle
**Date:** 2026-02-26
**Epic:** E1 — Project Scaffolding

## Aspire 13.1.2 via NuGet (not workload)

**What:** Aspire is consumed as NuGet packages, not via `dotnet workload install aspire`.
**Why:** The Aspire workload is deprecated in .NET 10. Templates installed via `dotnet new install Aspire.ProjectTemplates`. All Aspire functionality comes from `Aspire.Hosting.*` and `Aspire.*` NuGet packages.

## Solution format: .slnx

**What:** The solution file is `WeRace.slnx`, not `.sln`.
**Why:** `.slnx` is the new default solution format in .NET 10. Simpler XML, smaller diff surface.

## AppHost dependency wiring

**What:** AppHost uses `WaitFor()` for PostgreSQL and Redis before starting the API.
**Why:** Prevents the API from starting before its dependencies are healthy. Aspire handles container lifecycle.

## Health check strategy

**What:** `/health` and `/alive` endpoints provided by ServiceDefaults. Aspire component packages (`Aspire.Npgsql`, `Aspire.StackExchange.Redis`) auto-register health checks for their respective services.
**Why:** Zero additional health check code needed. PostgreSQL and Redis connectivity is verified automatically.
