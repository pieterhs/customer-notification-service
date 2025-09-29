# CustomerNotificationService

A backend service for creating, scheduling, and delivering customer notifications across channels (email, SMS, push). Includes templating, queueing, retries, and auditing.

## Tech Stack
- .NET 8 (ASP.NET Core)
- EF Core (Npgsql / PostgreSQL)
- Serilog
- Swagger / OpenAPI

## Project Structure
- `src/CustomerNotificationService.Api` — ASP.NET Core Web API (controllers, DI, Serilog, Swagger)
- `src/CustomerNotificationService.Application` — Application layer (services, interfaces)
- `src/CustomerNotificationService.Domain` — Domain entities and enums
- `src/CustomerNotificationService.Infrastructure` — EF Core DbContext, repositories, providers, queue (in-memory)
- `src/CustomerNotificationService.Workers` — Background workers (queue processor and scheduler)
- `tests/CustomerNotificationService.Tests` — xUnit tests (service and template basics)
- `docs/` — API contract and schema

## Prereqs
- .NET SDK 8.0+
- PostgreSQL (optional for now; default connection string points to localhost)

## Configure
Set connection string via either `appsettings.json` or environment variable `POSTGRES_CONNECTION`.

## Build
Run from repo root:

```powershell
dotnet build
```

## Run
- API:

```powershell
dotnet run --project .\src\CustomerNotificationService.Api\CustomerNotificationService.Api.csproj
```

- Workers:

```powershell
dotnet run --project .\src\CustomerNotificationService.Workers\CustomerNotificationService.Workers.csproj
```

Swagger UI will be available at http://localhost:5000/swagger in Development.

## Test

```powershell
dotnet test -v minimal
```

## Notes
- Queue service is currently in-memory (for scaffolding).
- EF Core model created, but migrations and database initialization are not included yet.
