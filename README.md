# Prezentownik Web API

Backend for Prezentownik — a small ASP.NET Core (minimal APIs) + EF Core + PostgreSQL service that lets
authenticated users create shareable gift lists, and lets anyone with the share link view and claim gifts.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for PostgreSQL, and optionally to run the whole stack)
- The [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) global tool, if you need to create/apply
  migrations yourself: `dotnet tool install --global dotnet-ef`

## Running locally

### 1. Start PostgreSQL (and optionally the API) with Docker Compose

```powershell
docker compose up -d
```

This starts:
- `postgres` — PostgreSQL 18 on port `5432` (user/password `postgres`, database `prezentownik`).
- `api` — the Web API itself, built from `src/`, listening on port `8080`.

If you only want the database (e.g. because you're running/debugging the API from your IDE instead), start
just that service:

```powershell
docker compose up -d postgres
```

### 2. Run the API from the CLI (alternative to the `api` container)

```powershell
cd src/Prezentownik.WebApi
dotnet run
```

The default connection string in `appsettings.json` points at `127.0.0.1:5432`, matching the `postgres`
service above, so no extra configuration is needed for local development.

### 3. Apply database migrations

Migrations are not applied automatically on startup, so run this once against a fresh database (and again
whenever a new migration is added):

```powershell
cd src/Prezentownik.WebApi
dotnet ef database update
```

## Running tests

```powershell
dotnet test
```

This runs the unit/integration tests in `tests/Prezentownik.WebApi.Tests` (using EF Core's in-memory
provider, so no database is required).

## Useful endpoints

- `GET /health` — health check (verifies database connectivity).
- `GET /openapi/v1.json` — OpenAPI document (available when running in the `Development` environment).
  A static, always-up-to-date copy is also generated on every build at `docs/openapi.json`, which the
  frontend uses to generate its TypeScript API types.

## Project layout

- `src/Prezentownik.WebApi/Modules` — feature modules (`Auth`, `UserLists`, `Public`), each exposing its own
  minimal API endpoints, DTOs, and mappers.
- `src/Prezentownik.WebApi/Models` — EF Core domain entities (`GiftList`, `Item`, `GiftClaim`, `AppUser`).
- `src/Prezentownik.WebApi/Migrations` — EF Core migrations.
- `tests/Prezentownik.WebApi.Tests` — xUnit tests.
