# WeRace

Formula 1 mobile companion app with historical race data, live info, and AI-powered Q&A.

## Tech Stack

- **Frontend:** React Native with React Native Paper
- **Backend:** .NET 10 Minimal API
- **Orchestration:** .NET Aspire (local development)
- **Database:** PostgreSQL
- **Cache:** Redis

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (required for PostgreSQL and Redis containers)

## Getting Started

### Backend (API + PostgreSQL + Redis)

Run the Aspire AppHost to start the API, PostgreSQL, and Redis together:

```bash
dotnet run --project src/api/WeRace.AppHost
```

This starts:

- **WeRace API** — .NET 10 Minimal API with health checks and OpenTelemetry
- **PostgreSQL** — database container with a `werace` database
- **Redis** — cache container

The Aspire dashboard opens automatically in your browser, showing all services, logs, and traces.

### Health Check

Once running, verify the API is healthy:

```bash
curl https://localhost:5150/health
```

## Project Structure

```
src/
  api/
    WeRace.AppHost/          # Aspire orchestrator — wires up all services
    WeRace.ServiceDefaults/  # Shared defaults: OpenTelemetry, health checks, service discovery
    WeRace.Api/              # .NET 10 Minimal API
```
